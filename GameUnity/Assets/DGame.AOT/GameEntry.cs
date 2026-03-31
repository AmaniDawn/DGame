using Cysharp.Threading.Tasks;
using DGame;
using UnityEngine;

public class GameEntry : MonoBehaviour
{
    private void Awake()
    {
        ModuleSystem.GetModule<IMonoDriver>();
        ModuleSystem.GetModule<IResourceModule>();
        ModuleSystem.GetModule<IFsmModule>();
        Settings.ProcedureSettings.StartProcedure().Forget();
        DontDestroyOnLoad(this);
    }
}