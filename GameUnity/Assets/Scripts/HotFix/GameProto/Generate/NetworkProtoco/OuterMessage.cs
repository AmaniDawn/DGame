using LightProto;
using System;
using MemoryPack;
using System.Collections.Generic;
using Fantasy;
using Fantasy.Pool;
using Fantasy.Network.Interface;
using Fantasy.Serialize;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618
// ReSharper disable InconsistentNaming
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable PreferConcreteValueOverDefault
// ReSharper disable RedundantNameQualifier
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable CheckNamespace
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable RedundantUsingDirective
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
namespace Fantasy
{
    /// <summary>
    /// 活动开启配置
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class CSActivityOpenEntry : AMessage, IDisposable
    {
        public static CSActivityOpenEntry Create(bool autoReturn = true)
        {
            var cSActivityOpenEntry = MessageObjectPool<CSActivityOpenEntry>.Rent();
            cSActivityOpenEntry.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                cSActivityOpenEntry.SetIsPool(false);
            }
            
            return cSActivityOpenEntry;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ActivityId = default;
            ActivityType = default;
            OpenTime = default;
            EndTime = default;
            DelayTime = default;
            MessageObjectPool<CSActivityOpenEntry>.Return(this);
        }
        /// <summary>
        /// 活动ID
        /// </summary>
        [ProtoMember(1)]
        public int ActivityId { get; set; }
        /// <summary>
        /// 活动类型
        /// </summary>
        [ProtoMember(2)]
        public int ActivityType { get; set; }
        /// <summary>
        /// 开始时间
        /// </summary>
        [ProtoMember(3)]
        public uint OpenTime { get; set; }
        /// <summary>
        /// 结束时间
        /// </summary>
        [ProtoMember(4)]
        public uint EndTime { get; set; }
        /// <summary>
        /// 延迟消失时间
        /// </summary>
        [ProtoMember(5)]
        public uint DelayTime { get; set; }
    }
    /// <summary>
    /// 客户端同步帧数据
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2S_SyncFrameDataReq : AMessage, IMessage
    {
        public static C2S_SyncFrameDataReq Create(bool autoReturn = true)
        {
            var c2S_SyncFrameDataReq = MessageObjectPool<C2S_SyncFrameDataReq>.Rent();
            c2S_SyncFrameDataReq.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2S_SyncFrameDataReq.SetIsPool(false);
            }
            
            return c2S_SyncFrameDataReq;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ForecastFrameId = default;
            RevClientFrameId = default;
            if (RoomInfo != null)
            {
                RoomInfo.Dispose();
                RoomInfo = null;
            }
            RoomPlayerId = default;
            if (FrameData != null)
            {
                FrameData.Dispose();
                FrameData = null;
            }
            MessageObjectPool<C2S_SyncFrameDataReq>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2S_SyncFrameDataReq; } 
        [ProtoMember(1)]
        public int ForecastFrameId { get; set; }
        [ProtoMember(2)]
        public int RevClientFrameId { get; set; }
        [ProtoMember(3)]
        public CSRoomInfo RoomInfo { get; set; }
        [ProtoMember(4)]
        public int RoomPlayerId { get; set; }
        [ProtoMember(5)]
        public CSOnePlayerFrameCmd FrameData { get; set; }
    }
    /// <summary>
    /// 客户端结束战斗 上报数据
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class S2C_BattleFinClientData : AMessage, IMessage
    {
        public static S2C_BattleFinClientData Create(bool autoReturn = true)
        {
            var s2C_BattleFinClientData = MessageObjectPool<S2C_BattleFinClientData>.Rent();
            s2C_BattleFinClientData.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                s2C_BattleFinClientData.SetIsPool(false);
            }
            
            return s2C_BattleFinClientData;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            if (StartParam != null)
            {
                StartParam.Dispose();
                StartParam = null;
            }
            DurationTime = default;
            MessageObjectPool<S2C_BattleFinClientData>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.S2C_BattleFinClientData; } 
        [ProtoMember(1)]
        public CSBattleStartParam StartParam { get; set; }
        [ProtoMember(2)]
        public uint DurationTime { get; set; }
    }
    /// <summary>
    /// 开始一场战斗的参数
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class CSBattleStartParam : AMessage, IDisposable
    {
        public static CSBattleStartParam Create(bool autoReturn = true)
        {
            var cSBattleStartParam = MessageObjectPool<CSBattleStartParam>.Rent();
            cSBattleStartParam.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                cSBattleStartParam.SetIsPool(false);
            }
            
            return cSBattleStartParam;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            RandSeed = default;
            Fps = default;
            PlayerCount = default;
            BattleStatus = default;
            IsGuide = default;
            StartTime = default;
            BattleGID = default;
            MultiPlayerBattle = default;
            CaptainPlayerId = default;
            LastOnlyBattleTime = default;
            MessageObjectPool<CSBattleStartParam>.Return(this);
        }
        [ProtoMember(1)]
        public int RandSeed { get; set; }
        [ProtoMember(2)]
        public int Fps { get; set; }
        [ProtoMember(3)]
        public int PlayerCount { get; set; }
        [ProtoMember(4)]
        public int BattleStatus { get; set; }
        [ProtoMember(5)]
        public byte IsGuide { get; set; }
        [ProtoMember(6)]
        public uint StartTime { get; set; }
        [ProtoMember(7)]
        public ulong BattleGID { get; set; }
        [ProtoMember(8)]
        public byte MultiPlayerBattle { get; set; }
        [ProtoMember(9)]
        public ulong CaptainPlayerId { get; set; }
        [ProtoMember(10)]
        public uint LastOnlyBattleTime { get; set; }
    }
    /// <summary>
    /// 服务器通知进入战斗
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class S2C_NotifyEnterBattle : AMessage, IMessage
    {
        public static S2C_NotifyEnterBattle Create(bool autoReturn = true)
        {
            var s2C_NotifyEnterBattle = MessageObjectPool<S2C_NotifyEnterBattle>.Rent();
            s2C_NotifyEnterBattle.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                s2C_NotifyEnterBattle.SetIsPool(false);
            }
            
            return s2C_NotifyEnterBattle;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            RandSeed = default;
            PlayerCount = default;
            IsHaveRoomInfo = default;
            RoomInfoList.Clear();
            BattleStatus = default;
            IsGuide = default;
            StartTime = default;
            BattleGID = default;
            MultiPlayerBattle = default;
            CaptainPlayerId = default;
            MessageObjectPool<S2C_NotifyEnterBattle>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.S2C_NotifyEnterBattle; } 
        [ProtoMember(1)]
        public int RandSeed { get; set; }
        [ProtoMember(2)]
        public int PlayerCount { get; set; }
        [ProtoMember(3)]
        public byte IsHaveRoomInfo { get; set; }
        [ProtoMember(4)]
        public List<CSRoomInfo> RoomInfoList { get; set; } = new List<CSRoomInfo>();
        [ProtoMember(5)]
        public int BattleStatus { get; set; }
        [ProtoMember(6)]
        public byte IsGuide { get; set; }
        [ProtoMember(7)]
        public uint StartTime { get; set; }
        [ProtoMember(8)]
        public ulong BattleGID { get; set; }
        [ProtoMember(9)]
        public byte MultiPlayerBattle { get; set; }
        [ProtoMember(10)]
        public ulong CaptainPlayerId { get; set; }
    }
    /// <summary>
    /// 服务器同步帧数据
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class S2C_BroadcastFrameData : AMessage, IMessage
    {
        public static S2C_BroadcastFrameData Create(bool autoReturn = true)
        {
            var s2C_BroadcastFrameData = MessageObjectPool<S2C_BroadcastFrameData>.Rent();
            s2C_BroadcastFrameData.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                s2C_BroadcastFrameData.SetIsPool(false);
            }
            
            return s2C_BroadcastFrameData;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            if (RoomInfo != null)
            {
                RoomInfo.Dispose();
                RoomInfo = null;
            }
            SveFrameId = default;
            FrameCount = default;
            FrameDataList.Clear();
            MessageObjectPool<S2C_BroadcastFrameData>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.S2C_BroadcastFrameData; } 
        [ProtoMember(1)]
        public CSRoomInfo RoomInfo { get; set; }
        [ProtoMember(2)]
        public int SveFrameId { get; set; }
        [ProtoMember(3)]
        public int FrameCount { get; set; }
        [ProtoMember(4)]
        public List<CSSyncOneFrameData> FrameDataList { get; set; } = new List<CSSyncOneFrameData>();
    }
    /// <summary>
    /// 房间信息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class CSRoomInfo : AMessage, IDisposable
    {
        public static CSRoomInfo Create(bool autoReturn = true)
        {
            var cSRoomInfo = MessageObjectPool<CSRoomInfo>.Rent();
            cSRoomInfo.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                cSRoomInfo.SetIsPool(false);
            }
            
            return cSRoomInfo;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            RoomId = default;
            RoomSeq = default;
            MessageObjectPool<CSRoomInfo>.Return(this);
        }
        [ProtoMember(1)]
        public int RoomId { get; set; }
        [ProtoMember(2)]
        public int RoomSeq { get; set; }
    }
    /// <summary>
    /// 单帧数据
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class CSSyncOneFrameData : AMessage, IDisposable
    {
        public static CSSyncOneFrameData Create(bool autoReturn = true)
        {
            var cSSyncOneFrameData = MessageObjectPool<CSSyncOneFrameData>.Rent();
            cSSyncOneFrameData.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                cSSyncOneFrameData.SetIsPool(false);
            }
            
            return cSSyncOneFrameData;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            FrameId = default;
            SpeedUp = default;
            PlayerCount = default;
            PlayerFrameData.Clear();
            MessageObjectPool<CSSyncOneFrameData>.Return(this);
        }
        [ProtoMember(1)]
        public int FrameId { get; set; }
        [ProtoMember(2)]
        public int SpeedUp { get; set; }
        [ProtoMember(3)]
        public int PlayerCount { get; set; }
        [ProtoMember(4)]
        public List<CSOnePlayerFrameCmd> PlayerFrameData { get; set; } = new List<CSOnePlayerFrameCmd>();
    }
    /// <summary>
    /// 单玩家帧操作数据
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class CSOnePlayerFrameCmd : AMessage, IDisposable
    {
        public static CSOnePlayerFrameCmd Create(bool autoReturn = true)
        {
            var cSOnePlayerFrameCmd = MessageObjectPool<CSOnePlayerFrameCmd>.Rent();
            cSOnePlayerFrameCmd.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                cSOnePlayerFrameCmd.SetIsPool(false);
            }
            
            return cSOnePlayerFrameCmd;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            PlayerId = default;
            FrameCmdCount = default;
            FrameDataList.Clear();
            MessageObjectPool<CSOnePlayerFrameCmd>.Return(this);
        }
        [ProtoMember(1)]
        public int PlayerId { get; set; }
        [ProtoMember(2)]
        public int FrameCmdCount { get; set; }
        [ProtoMember(3)]
        public List<CSFrameCmd> FrameDataList { get; set; } = new List<CSFrameCmd>();
    }
    /// <summary>
    /// 帧命令
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class CSFrameCmd : AMessage, IDisposable
    {
        public static CSFrameCmd Create(bool autoReturn = true)
        {
            var cSFrameCmd = MessageObjectPool<CSFrameCmd>.Rent();
            cSFrameCmd.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                cSFrameCmd.SetIsPool(false);
            }
            
            return cSFrameCmd;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            Type = default;
            if (Data != null)
            {
                Data.Dispose();
                Data = null;
            }
            MessageObjectPool<CSFrameCmd>.Return(this);
        }
        [ProtoMember(1)]
        public byte Type { get; set; }
        [ProtoMember(2)]
        public CSFrameData Data { get; set; }
    }
    /// <summary>
    /// 帧命令
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class CSFrameData : AMessage, IDisposable
    {
        public static CSFrameData Create(bool autoReturn = true)
        {
            var cSFrameData = MessageObjectPool<CSFrameData>.Rent();
            cSFrameData.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                cSFrameData.SetIsPool(false);
            }
            
            return cSFrameData;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            if (Gm != null)
            {
                Gm.Dispose();
                Gm = null;
            }
            MessageObjectPool<CSFrameData>.Return(this);
        }
        [ProtoMember(1)]
        public CSFrameCmdGmInfo Gm { get; set; }
    }
    /// <summary>
    /// GM命令
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class CSFrameCmdGmInfo : AMessage, IDisposable
    {
        public static CSFrameCmdGmInfo Create(bool autoReturn = true)
        {
            var cSFrameCmdGmInfo = MessageObjectPool<CSFrameCmdGmInfo>.Rent();
            cSFrameCmdGmInfo.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                cSFrameCmdGmInfo.SetIsPool(false);
            }
            
            return cSFrameCmdGmInfo;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            GmString = default;
            MessageObjectPool<CSFrameCmdGmInfo>.Return(this);
        }
        [ProtoMember(1)]
        public string GmString { get; set; }
    }
    /// <summary>
    /// 注册账号协议
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2A_RegisterRequest : AMessage, IRequest
    {
        public static C2A_RegisterRequest Create(bool autoReturn = true)
        {
            var c2A_RegisterRequest = MessageObjectPool<C2A_RegisterRequest>.Rent();
            c2A_RegisterRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2A_RegisterRequest.SetIsPool(false);
            }
            
            return c2A_RegisterRequest;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            UserName = default;
            Password = default;
            MessageObjectPool<C2A_RegisterRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2A_RegisterRequest; } 
        [ProtoIgnore]
        public A2C_RegisterResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string UserName { get; set; }
        [ProtoMember(2)]
        public string Password { get; set; }
    }
    /// <summary>
    /// 注册账号协议返回
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class A2C_RegisterResponse : AMessage, IResponse
    {
        public static A2C_RegisterResponse Create(bool autoReturn = true)
        {
            var a2C_RegisterResponse = MessageObjectPool<A2C_RegisterResponse>.Rent();
            a2C_RegisterResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                a2C_RegisterResponse.SetIsPool(false);
            }
            
            return a2C_RegisterResponse;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            MessageObjectPool<A2C_RegisterResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.A2C_RegisterResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// 登录账号协议
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2A_LoginRequest : AMessage, IRequest
    {
        public static C2A_LoginRequest Create(bool autoReturn = true)
        {
            var c2A_LoginRequest = MessageObjectPool<C2A_LoginRequest>.Rent();
            c2A_LoginRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2A_LoginRequest.SetIsPool(false);
            }
            
            return c2A_LoginRequest;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            UserName = default;
            Password = default;
            LoginType = default;
            MessageObjectPool<C2A_LoginRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2A_LoginRequest; } 
        [ProtoIgnore]
        public A2C_LoginResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string UserName { get; set; }
        [ProtoMember(2)]
        public string Password { get; set; }
        [ProtoMember(3)]
        public uint LoginType { get; set; }
    }
    /// <summary>
    /// 登录账号协议返回
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class A2C_LoginResponse : AMessage, IResponse
    {
        public static A2C_LoginResponse Create(bool autoReturn = true)
        {
            var a2C_LoginResponse = MessageObjectPool<A2C_LoginResponse>.Rent();
            a2C_LoginResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                a2C_LoginResponse.SetIsPool(false);
            }
            
            return a2C_LoginResponse;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            RoleID = default;
            Token = default;
            ServerInfoList = null;
            RecentServerList = null;
            RecentServerRoleInfoList = null;
            MessageObjectPool<A2C_LoginResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.A2C_LoginResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public long RoleID { get; set; }
        [ProtoMember(3)]
        public string Token { get; set; }
        [ProtoMember(4)]
        public List<CSServerInfo> ServerInfoList { get; set; }
        [ProtoMember(5)]
        public List<int> RecentServerList { get; set; }
        [ProtoMember(6)]
        public List<CSRecentServerRoleInfo> RecentServerRoleInfoList { get; set; }
    }
    /// <summary>
    /// 服务器信息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class CSServerInfo : AMessage, IDisposable
    {
        public static CSServerInfo Create(bool autoReturn = true)
        {
            var cSServerInfo = MessageObjectPool<CSServerInfo>.Rent();
            cSServerInfo.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                cSServerInfo.SetIsPool(false);
            }
            
            return cSServerInfo;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ServerID = default;
            Name = default;
            Group = default;
            Address = default;
            Port = default;
            Recommend = default;
            State = default;
            MessageObjectPool<CSServerInfo>.Return(this);
        }
        /// <summary>
        /// 服务器ID
        /// </summary>
        [ProtoMember(1)]
        public int ServerID { get; set; }
        /// <summary>
        /// 服务器名字
        /// </summary>
        [ProtoMember(2)]
        public string Name { get; set; }
        /// <summary>
        /// 服务器分组
        /// </summary>
        [ProtoMember(3)]
        public int Group { get; set; }
        /// <summary>
        /// 服务器地址
        /// </summary>
        [ProtoMember(4)]
        public string Address { get; set; }
        /// <summary>
        /// 服务器端口号
        /// </summary>
        [ProtoMember(5)]
        public int Port { get; set; }
        /// <summary>
        /// 是否是推荐服务器
        /// </summary>
        [ProtoMember(6)]
        public bool Recommend { get; set; }
        /// <summary>
        /// 服务器状态
        /// </summary>
        [ProtoMember(7)]
        public byte State { get; set; }
    }
    /// <summary>
    /// 最近登录服务器的角色摘要
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class CSRecentServerRoleInfo : AMessage, IDisposable
    {
        public static CSRecentServerRoleInfo Create(bool autoReturn = true)
        {
            var cSRecentServerRoleInfo = MessageObjectPool<CSRecentServerRoleInfo>.Rent();
            cSRecentServerRoleInfo.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                cSRecentServerRoleInfo.SetIsPool(false);
            }
            
            return cSRecentServerRoleInfo;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ServerID = default;
            Level = default;
            MessageObjectPool<CSRecentServerRoleInfo>.Return(this);
        }
        /// <summary>
        /// 服务器ID
        /// </summary>
        [ProtoMember(1)]
        public int ServerID { get; set; }
        /// <summary>
        /// 角色等级
        /// </summary>
        [ProtoMember(2)]
        public uint Level { get; set; }
    }
    /// <summary>
    /// 同步当前选择的服务器
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2A_RecordRecentServer : AMessage, IMessage
    {
        public static C2A_RecordRecentServer Create(bool autoReturn = true)
        {
            var c2A_RecordRecentServer = MessageObjectPool<C2A_RecordRecentServer>.Rent();
            c2A_RecordRecentServer.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2A_RecordRecentServer.SetIsPool(false);
            }
            
            return c2A_RecordRecentServer;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            RoleID = default;
            ServerID = default;
            MessageObjectPool<C2A_RecordRecentServer>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2A_RecordRecentServer; } 
        [ProtoMember(1)]
        public long RoleID { get; set; }
        /// <summary>
        /// 服务器ID
        /// </summary>
        [ProtoMember(2)]
        public int ServerID { get; set; }
    }
    /// <summary>
    /// 客户端登录到Gate服务器
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_LoginRequest : AMessage, IRequest
    {
        public static C2G_LoginRequest Create(bool autoReturn = true)
        {
            var c2G_LoginRequest = MessageObjectPool<C2G_LoginRequest>.Rent();
            c2G_LoginRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_LoginRequest.SetIsPool(false);
            }
            
            return c2G_LoginRequest;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            Token = default;
            ServerID = default;
            MessageObjectPool<C2G_LoginRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_LoginRequest; } 
        [ProtoIgnore]
        public G2C_LoginResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string Token { get; set; }
        [ProtoMember(2)]
        public int ServerID { get; set; }
    }
    /// <summary>
    /// 登录协议返回
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_LoginResponse : AMessage, IResponse
    {
        public static G2C_LoginResponse Create(bool autoReturn = true)
        {
            var g2C_LoginResponse = MessageObjectPool<G2C_LoginResponse>.Rent();
            g2C_LoginResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_LoginResponse.SetIsPool(false);
            }
            
            return g2C_LoginResponse;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            if (PlayerData != null)
            {
                PlayerData.Dispose();
                PlayerData = null;
            }
            MessageObjectPool<G2C_LoginResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_LoginResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public CSPlayerData PlayerData { get; set; }
    }
    /// <summary>
    /// 通知客户端重复登录
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_RepeatLogin : AMessage, IMessage
    {
        public static G2C_RepeatLogin Create(bool autoReturn = true)
        {
            var g2C_RepeatLogin = MessageObjectPool<G2C_RepeatLogin>.Rent();
            g2C_RepeatLogin.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_RepeatLogin.SetIsPool(false);
            }
            
            return g2C_RepeatLogin;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            MessageObjectPool<G2C_RepeatLogin>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_RepeatLogin; } 
    }
    /// <summary>
    /// 玩家角色基础数据
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class CSPlayerData : AMessage, IDisposable
    {
        public static CSPlayerData Create(bool autoReturn = true)
        {
            var cSPlayerData = MessageObjectPool<CSPlayerData>.Rent();
            cSPlayerData.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                cSPlayerData.SetIsPool(false);
            }
            
            return cSPlayerData;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            RoleID = default;
            RoleNo = default;
            RoleName = default;
            HeadID = default;
            Sex = default;
            Level = default;
            Exp = default;
            FightValue = default;
            Diamond = default;
            Gold = default;
            Stam = default;
            LastLoginTime = default;
            CreateTime = default;
            IsFinGuide = default;
            Sign = default;
            WorldID = default;
            TotalRmb = default;
            LastAddStamTime = default;
            DailyBuyStamCount = default;
            MessageObjectPool<CSPlayerData>.Return(this);
        }
        /// <summary>
        /// RoleID
        /// </summary>
        [ProtoMember(1)]
        public ulong RoleID { get; set; }
        /// <summary>
        /// RoleNO
        /// </summary>
        [ProtoMember(2)]
        public ulong RoleNo { get; set; }
        /// <summary>
        /// 角色名称
        /// </summary>
        [ProtoMember(3)]
        public string RoleName { get; set; }
        /// <summary>
        /// 头像ID
        /// </summary>
        [ProtoMember(4)]
        public int HeadID { get; set; }
        /// <summary>
        /// 性别
        /// </summary>
        [ProtoMember(5)]
        public byte Sex { get; set; }
        /// <summary>
        /// 等级
        /// </summary>
        [ProtoMember(6)]
        public uint Level { get; set; }
        /// <summary>
        /// 经验
        /// </summary>
        [ProtoMember(7)]
        public uint Exp { get; set; }
        /// <summary>
        /// 战斗力
        /// </summary>
        [ProtoMember(8)]
        public uint FightValue { get; set; }
        /// <summary>
        /// 钻石
        /// </summary>
        [ProtoMember(9)]
        public uint Diamond { get; set; }
        /// <summary>
        /// 金币
        /// </summary>
        [ProtoMember(10)]
        public uint Gold { get; set; }
        /// <summary>
        /// 体力
        /// </summary>
        [ProtoMember(11)]
        public uint Stam { get; set; }
        /// <summary>
        /// 上次登录时间
        /// </summary>
        [ProtoMember(12)]
        public long LastLoginTime { get; set; }
        /// <summary>
        /// 创角时间
        /// </summary>
        [ProtoMember(13)]
        public long CreateTime { get; set; }
        /// <summary>
        /// 是否完成新手引导
        /// </summary>
        [ProtoMember(14)]
        public byte IsFinGuide { get; set; }
        /// <summary>
        /// 个性签名
        /// </summary>
        [ProtoMember(15)]
        public string Sign { get; set; }
        /// <summary>
        /// 所在主服
        /// </summary>
        [ProtoMember(16)]
        public int WorldID { get; set; }
        /// <summary>
        /// 累计充值金额
        /// </summary>
        [ProtoMember(17)]
        public uint TotalRmb { get; set; }
        /// <summary>
        /// 上次增加体力时间
        /// </summary>
        [ProtoMember(18)]
        public long LastAddStamTime { get; set; }
        /// <summary>
        /// 每日购买体力次数
        /// </summary>
        [ProtoMember(19)]
        public int DailyBuyStamCount { get; set; }
    }
}