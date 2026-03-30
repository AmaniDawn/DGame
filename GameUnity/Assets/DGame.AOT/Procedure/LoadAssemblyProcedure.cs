using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DGame.AOT;
using Cysharp.Threading.Tasks;
using DGame;
using UnityEngine;
using YooAsset;

#if ENABLE_HYBRIDCLR

using HybridCLR;

#endif

#if ENABLE_OBFUZ
using Obfuz;
using Obfuz.EncryptionVM;
#endif

namespace Procedure
{
    /// <summary>
    /// 10-加载DLL
    /// </summary>
    public class LoadAssemblyProcedure : ProcedureBase
    {
        public override bool UseNativeDialog => true;
        private bool m_enableAddressable = true;
        private int m_loadAssetCnt;
        private int m_loadMetadataAssetCnt;
        private int m_failureAssetCnt;
        private int m_failureMetadataAssetCnt;
        private bool m_loadAssemblyComplete;
        private bool m_loadMetadataAssemblyComplete;
        private bool m_loadAssemblyWait;
        private bool m_loadMetadataAssetWait;
        private Assembly m_mainLogicAssembly;
        private List<Assembly> m_hotfixAssemblyList;
        private UpdateSettings m_updateSettings;
        private HashSet<string> m_cachedHotUpdateDllSet;

        public override void OnCreate(IFsm<IProcedureModule> fsm)
        {
            base.OnCreate(fsm);
            m_updateSettings = Settings.UpdateSettings;
            m_enableAddressable = m_updateSettings.EnableAddressable;
        }

        public override void OnEnter()
        {
            // DLogger.Info("======== 10-加载DLL流程 ========");

#if ENABLE_HYBRIDCLR

            // 开始加载DLL之前先执行一下优化
            HybridCLROptimizer.OptimizeHybridCLR();

#endif

            LoadAssembly().Forget();
        }

        private async UniTaskVoid LoadAssembly()
        {
            m_loadAssemblyComplete = false;
            m_hotfixAssemblyList = new List<Assembly>();

            if (m_updateSettings.Enable)
            {
#if !UNITY_EDITOR
                m_loadMetadataAssemblyComplete = false;
                LoadMetadataForAotAssembly();
#else
                m_loadMetadataAssemblyComplete = true;
#endif
            }
            else
            {
                m_loadMetadataAssemblyComplete = true;
            }

            if (!m_updateSettings.Enable || m_resourceModule.PlayMode == EPlayMode.EditorSimulateMode)
            {
                m_mainLogicAssembly = GetMainLogicAssembly();
            }
            else
            {
                if (m_updateSettings.Enable)
                {
                    foreach (var hotfixDllName in m_updateSettings.HotUpdateAssemblies)
                    {
                        var assetLocation = hotfixDllName;
                        if (!m_enableAddressable)
                        {
                            assetLocation = Utility.PathUtil.GetRegularPath(
                                Path.Combine("Assets", m_updateSettings.AssemblyTextAssetPath,
                                    $"{hotfixDllName}{m_updateSettings.AssemblyTextAssetExtension}"));
                        }
                        DLogger.Log($"======== 加载DLL资源: {assetLocation} ========");
                        m_loadAssetCnt++;
                        var result = await m_resourceModule.LoadAssetAsync<TextAsset>(assetLocation);
                        LoadAssetSuccess(result);
                    }
                    m_loadAssemblyWait = true;
                }
                else
                {
                    m_mainLogicAssembly = GetMainLogicAssembly();
                }
            }

            if (m_loadAssetCnt == 0)
            {
                m_loadAssemblyComplete = true;
            }
        }

        private void LoadAssetSuccess(TextAsset textAsset)
        {
            m_loadAssetCnt--;

            if (textAsset == null)
            {
                DLogger.Warning("加载热更DLL失败");
                return;
            }
            var assetName = textAsset.name;
            DLogger.Log($"加载热更DLL资源成功: [{assetName}]");

            try
            {
                var assembly = Assembly.Load(textAsset.bytes);

                if (string.Equals(m_updateSettings.LogicMainDllName, assetName, StringComparison.Ordinal))
                {
                    m_mainLogicAssembly = assembly;
                }
                m_hotfixAssemblyList.Add(assembly);
                DLogger.Log($"热更DLL加载成功: [{assembly.GetName().Name}]");
            }
            catch (Exception e)
            {
                m_failureAssetCnt++;
                DLogger.Fatal(e);
                throw;
            }
            finally
            {
                m_loadAssemblyComplete = m_loadAssemblyWait && m_loadAssetCnt == 0;
            }
            m_resourceModule.UnloadAsset(textAsset);
        }

        private Assembly GetMainLogicAssembly()
        {
            m_hotfixAssemblyList.Clear();
            Assembly mainLogicAssembly = null;

            if (m_cachedHotUpdateDllSet == null)
            {
                m_cachedHotUpdateDllSet = new HashSet<string>(m_updateSettings.HotUpdateAssemblies);
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string assemblyName = $"{assembly.GetName().Name}.dll";
                // 从当前域中查找到主业务逻辑DLL
                if (string.Equals(m_updateSettings.LogicMainDllName, assemblyName,
                        StringComparison.Ordinal))
                {
                    mainLogicAssembly = assembly;
                }

                // 查找所有已经加载的热更的DLL
                if (m_cachedHotUpdateDllSet.Contains(assemblyName))
                {
                    m_hotfixAssemblyList.Add(assembly);
                    m_cachedHotUpdateDllSet.Remove(assemblyName);
                }

                if (mainLogicAssembly != null &&
                    m_cachedHotUpdateDllSet.Count <= 0)
                {
                    break;
                }
            }
            return mainLogicAssembly;
        }

        /// <summary>
        /// 为Aot Assembly加载原始metadata， 这个代码放Aot或者热更新都行。
        /// 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行。
        /// </summary>
        private void LoadMetadataForAotAssembly()
        {
            // 可以加载任意aot assembly的对应的dll。但要求dll必须与unity build过程中生成的裁剪后的dll一致，而不能直接使用原始dll。
            // 我们在BuildProcessor_xxx里添加了处理代码，这些裁剪后的dll在打包时自动被复制到 {项目目录}/HybridCLRData/AssembliesPostIl2CppStrip/{Target} 目录。

            // 注意，补充元数据是给AOT dll补充元数据，而不是给热更新dll补充元数据。
            // 热更新dll不缺元数据，不需要补充，如果调用LoadMetadataForAOTAssembly会返回错误
            if (m_updateSettings.AOTMetaAssemblies.Count == 0)
            {
                m_loadMetadataAssemblyComplete = true;
                return;
            }

            foreach (var aotDllName in m_updateSettings.AOTMetaAssemblies)
            {
                var assetLocation = aotDllName;
                if (!m_enableAddressable)
                {
                    assetLocation = Utility.PathUtil.GetRegularPath(
                        Path.Combine("Assets", m_updateSettings.AssemblyTextAssetPath,
                            $"{aotDllName}{m_updateSettings.AssemblyTextAssetExtension}"));
                }
                DLogger.Log($"加载AOT程序集: [{assetLocation}]");
                m_loadMetadataAssetCnt++;
                m_resourceModule.LoadAssetAsync<TextAsset>(assetLocation, LoadMetadataAssetSuccess);
            }

            m_loadMetadataAssetWait = true;
        }

        private void LoadMetadataAssetSuccess(TextAsset textAsset)
        {
            m_loadMetadataAssetCnt--;

            if (textAsset == null)
            {
                DLogger.Log("LoadMetadataAssetSuccess: 加载元数据资源失败");
                return;
            }
            string assetName = textAsset.name;
            DLogger.Log($"LoadMetadataAssetSuccess: 加载元数据资源成功: [{assetName}]");

            try
            {
                byte[] bytes = textAsset.bytes;

#if ENABLE_HYBRIDCLR

                var mode = HomologousImageMode.SuperSet;
                var err = (LoadImageErrorCode)HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(bytes, mode);
                DLogger.Warning($"LoadMetadataAssetSuccess: [{assetName}] mode:{mode} err:{err}");

#endif
            }
            catch (Exception e)
            {
                m_failureMetadataAssetCnt++;
                DLogger.Fatal(e.Message);
                throw;
            }
            finally
            {
                m_loadMetadataAssemblyComplete = m_loadMetadataAssetWait && m_loadMetadataAssetCnt == 0;
            }
            m_resourceModule.UnloadAsset(textAsset);
        }

        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            if (!m_loadAssemblyComplete)
            {
                return;
            }

            if (!m_loadMetadataAssemblyComplete)
            {
                return;
            }

            AllAssemblyLoadComplete();
        }

        private void AllAssemblyLoadComplete()
        {
            SetUpStaticSecretKey();
            SwitchState<StartGameProcedure>();

#if UNITY_EDITOR
            m_mainLogicAssembly = GetMainLogicAssembly();
#endif
            if (m_mainLogicAssembly == null)
            {
                DLogger.Fatal($"主业务逻辑DLL丢失 请检查设置里的宏定义 \'ENABLE_HYBRIDCLR\' 和是否存在文件=>{m_updateSettings.LogicMainDllName}.bytes");
                return;
            }

            var entryType = m_mainLogicAssembly.GetType("GameStart");
            if (entryType == null)
            {
                DLogger.Fatal("游戏主入口文件 'GameStart' 丢失");
                return;
            }
            var entryMethod = entryType.GetMethod("Entrance", BindingFlags.Static | BindingFlags.Public);
            if (entryMethod == null)
            {
                DLogger.Fatal("游戏主入口方法 'GameStart.Entrance' 丢失");
                return;
            }
            // await UniTask.Yield();
            object[] objects = new object[] { new object[] { m_hotfixAssemblyList } };
            entryMethod.Invoke(entryType, objects);
        }

        private void SetUpStaticSecretKey()
        {
#if ENABLE_OBFUZ
            DLogger.Info("Enable Obfuz");
            DLogger.Info("SetUpStaticSecret begin");
            EncryptionService<DefaultStaticEncryptionScope>.Encryptor =
                new GeneratedEncryptionVirtualMachine(Resources.Load<TextAsset>("Obfuz/defaultStaticSecretKey").bytes);
            DLogger.Info("SetUpStaticSecret end");
#else
            DLogger.Info("Disable Obfuz");
#endif
        }

        public override void OnFixedUpdate()
        {
        }

        public override void OnExit()
        {
        }

        public override void OnDestroy()
        {
        }
    }
}