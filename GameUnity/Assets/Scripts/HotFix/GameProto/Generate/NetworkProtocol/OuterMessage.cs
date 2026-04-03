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
    /// 创建房间请求
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_CreateRoomRequest : AMessage, IRequest
    {
        public static C2G_CreateRoomRequest Create(bool autoReturn = true)
        {
            var c2G_CreateRoomRequest = MessageObjectPool<C2G_CreateRoomRequest>.Rent();
            c2G_CreateRoomRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_CreateRoomRequest.SetIsPool(false);
            }
            
            return c2G_CreateRoomRequest;
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
            PlayerCount = default;
            MessageObjectPool<C2G_CreateRoomRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_CreateRoomRequest; } 
        [ProtoIgnore]
        public G2C_CreateRoomResponse ResponseType { get; set; }
        /// <summary>
        /// 房间初始玩家数量
        /// </summary>
        [ProtoMember(1)]
        public int PlayerCount { get; set; }
    }
    /// <summary>
    /// 创建房间返回
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_CreateRoomResponse : AMessage, IResponse
    {
        public static G2C_CreateRoomResponse Create(bool autoReturn = true)
        {
            var g2C_CreateRoomResponse = MessageObjectPool<G2C_CreateRoomResponse>.Rent();
            g2C_CreateRoomResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_CreateRoomResponse.SetIsPool(false);
            }
            
            return g2C_CreateRoomResponse;
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
            if (RoomInfo != null)
            {
                RoomInfo.Dispose();
                RoomInfo = null;
            }
            PlayerCount = default;
            PlayerInfos.Clear();
            MessageObjectPool<G2C_CreateRoomResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_CreateRoomResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        /// <summary>
        /// 创建出的房间信息
        /// </summary>
        [ProtoMember(2)]
        public CSRoomInfo RoomInfo { get; set; }
        /// <summary>
        /// 当前房间玩家数量
        /// </summary>
        [ProtoMember(3)]
        public int PlayerCount { get; set; }
        /// <summary>
        /// 当前房间玩家信息
        /// </summary>
        [ProtoMember(4)]
        public List<CSRoomPlayerInfo> PlayerInfos { get; set; } = new List<CSRoomPlayerInfo>();
    }
    /// <summary>
    /// 加入房间请求
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_JoinRoomRequest : AMessage, IRequest
    {
        public static C2G_JoinRoomRequest Create(bool autoReturn = true)
        {
            var c2G_JoinRoomRequest = MessageObjectPool<C2G_JoinRoomRequest>.Rent();
            c2G_JoinRoomRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_JoinRoomRequest.SetIsPool(false);
            }
            
            return c2G_JoinRoomRequest;
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
            MessageObjectPool<C2G_JoinRoomRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_JoinRoomRequest; } 
        [ProtoIgnore]
        public G2C_JoinRoomResponse ResponseType { get; set; }
        /// <summary>
        /// 要加入的房间ID
        /// </summary>
        [ProtoMember(1)]
        public int RoomId { get; set; }
    }
    /// <summary>
    /// 加入房间返回
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_JoinRoomResponse : AMessage, IResponse
    {
        public static G2C_JoinRoomResponse Create(bool autoReturn = true)
        {
            var g2C_JoinRoomResponse = MessageObjectPool<G2C_JoinRoomResponse>.Rent();
            g2C_JoinRoomResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_JoinRoomResponse.SetIsPool(false);
            }
            
            return g2C_JoinRoomResponse;
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
            if (RoomInfo != null)
            {
                RoomInfo.Dispose();
                RoomInfo = null;
            }
            PlayerCount = default;
            PlayerInfos.Clear();
            MessageObjectPool<G2C_JoinRoomResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_JoinRoomResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        /// <summary>
        /// 当前房间信息
        /// </summary>
        [ProtoMember(2)]
        public CSRoomInfo RoomInfo { get; set; }
        /// <summary>
        /// 当前房间玩家数量
        /// </summary>
        [ProtoMember(3)]
        public int PlayerCount { get; set; }
        /// <summary>
        /// 当前房间玩家信息
        /// </summary>
        [ProtoMember(4)]
        public List<CSRoomPlayerInfo> PlayerInfos { get; set; } = new List<CSRoomPlayerInfo>();
    }
    /// <summary>
    /// 离开房间请求
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_LeaveRoomRequest : AMessage, IRequest
    {
        public static C2G_LeaveRoomRequest Create(bool autoReturn = true)
        {
            var c2G_LeaveRoomRequest = MessageObjectPool<C2G_LeaveRoomRequest>.Rent();
            c2G_LeaveRoomRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_LeaveRoomRequest.SetIsPool(false);
            }
            
            return c2G_LeaveRoomRequest;
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
            MessageObjectPool<C2G_LeaveRoomRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_LeaveRoomRequest; } 
        [ProtoIgnore]
        public G2C_LeaveRoomResponse ResponseType { get; set; }
    }
    /// <summary>
    /// 离开房间返回
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_LeaveRoomResponse : AMessage, IResponse
    {
        public static G2C_LeaveRoomResponse Create(bool autoReturn = true)
        {
            var g2C_LeaveRoomResponse = MessageObjectPool<G2C_LeaveRoomResponse>.Rent();
            g2C_LeaveRoomResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_LeaveRoomResponse.SetIsPool(false);
            }
            
            return g2C_LeaveRoomResponse;
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
            MessageObjectPool<G2C_LeaveRoomResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_LeaveRoomResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
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
        /// <summary>
        /// 客户端预测帧ID
        /// </summary>
        [ProtoMember(1)]
        public int ForecastFrameId { get; set; }
        /// <summary>
        /// 接收到的客户端帧ID
        /// </summary>
        [ProtoMember(2)]
        public int RevClientFrameId { get; set; }
        /// <summary>
        /// 房间信息
        /// </summary>
        [ProtoMember(3)]
        public CSRoomInfo RoomInfo { get; set; }
        /// <summary>
        /// 房间玩家ID
        /// </summary>
        [ProtoMember(4)]
        public int RoomPlayerId { get; set; }
        /// <summary>
        /// 帧数据
        /// </summary>
        [ProtoMember(5)]
        public CSOnePlayerFrameCmd FrameData { get; set; }
    }
    /// <summary>
    /// 客户端结束战斗 上报数据
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class S2C_BattleFinClientDataReq : AMessage, IMessage
    {
        public static S2C_BattleFinClientDataReq Create(bool autoReturn = true)
        {
            var s2C_BattleFinClientDataReq = MessageObjectPool<S2C_BattleFinClientDataReq>.Rent();
            s2C_BattleFinClientDataReq.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                s2C_BattleFinClientDataReq.SetIsPool(false);
            }
            
            return s2C_BattleFinClientDataReq;
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
            MessageObjectPool<S2C_BattleFinClientDataReq>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.S2C_BattleFinClientDataReq; } 
        /// <summary>
        /// 关卡的初始化数据，用于和服务器校验
        /// </summary>
        [ProtoMember(1)]
        public CSBattleStartParam StartParam { get; set; }
        /// <summary>
        /// 持续时间
        /// </summary>
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
        /// <summary>
        /// 随机种子
        /// </summary>
        [ProtoMember(1)]
        public int RandSeed { get; set; }
        /// <summary>
        /// 模拟帧率
        /// </summary>
        [ProtoMember(2)]
        public int Fps { get; set; }
        /// <summary>
        /// 玩家个数
        /// </summary>
        [ProtoMember(3)]
        public int PlayerCount { get; set; }
        /// <summary>
        /// 战斗状态
        /// </summary>
        [ProtoMember(4)]
        public int BattleStatus { get; set; }
        /// <summary>
        /// 是否有新手引导
        /// </summary>
        [ProtoMember(5)]
        public byte IsGuide { get; set; }
        /// <summary>
        /// 战斗开始时间
        /// </summary>
        [ProtoMember(6)]
        public uint StartTime { get; set; }
        /// <summary>
        /// 战斗GID
        /// </summary>
        [ProtoMember(7)]
        public ulong BattleGID { get; set; }
        /// <summary>
        /// 是否多人开始战斗
        /// </summary>
        [ProtoMember(8)]
        public byte MultiPlayerBattle { get; set; }
        /// <summary>
        /// 队长ID
        /// </summary>
        [ProtoMember(9)]
        public ulong CaptainPlayerId { get; set; }
        /// <summary>
        /// 剩余战斗时间
        /// </summary>
        [ProtoMember(10)]
        public uint LastOnlyBattleTime { get; set; }
    }
    /// <summary>
    /// 客户端开始战斗请求
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2S_StartBattleRequest : AMessage, IRequest
    {
        public static C2S_StartBattleRequest Create(bool autoReturn = true)
        {
            var c2S_StartBattleRequest = MessageObjectPool<C2S_StartBattleRequest>.Rent();
            c2S_StartBattleRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2S_StartBattleRequest.SetIsPool(false);
            }
            
            return c2S_StartBattleRequest;
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
            MessageObjectPool<C2S_StartBattleRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2S_StartBattleRequest; } 
        [ProtoIgnore]
        public S2C_StartBattleResponse ResponseType { get; set; }
    }
    /// <summary>
    /// 客户端开始战斗请求返回
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class S2C_StartBattleResponse : AMessage, IResponse
    {
        public static S2C_StartBattleResponse Create(bool autoReturn = true)
        {
            var s2C_StartBattleResponse = MessageObjectPool<S2C_StartBattleResponse>.Rent();
            s2C_StartBattleResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                s2C_StartBattleResponse.SetIsPool(false);
            }
            
            return s2C_StartBattleResponse;
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
            MessageObjectPool<S2C_StartBattleResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.S2C_StartBattleResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
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
            if (RoomInfoList != null)
            {
                RoomInfoList.Dispose();
                RoomInfoList = null;
            }
            BattleStatus = default;
            IsGuide = default;
            StartTime = default;
            BattleGID = default;
            MultiPlayerBattle = default;
            CaptainPlayerId = default;
            PlayerDataList.Clear();
            if (Chapter != null)
            {
                Chapter.Dispose();
                Chapter = null;
            }
            Stage = default;
            MapID = default;
            MessageObjectPool<S2C_NotifyEnterBattle>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.S2C_NotifyEnterBattle; } 
        /// <summary>
        /// 随机种子
        /// </summary>
        [ProtoMember(1)]
        public int RandSeed { get; set; }
        /// <summary>
        /// 玩家个数
        /// </summary>
        [ProtoMember(2)]
        public int PlayerCount { get; set; }
        /// <summary>
        /// 是否有房间信息
        /// </summary>
        [ProtoMember(3)]
        public byte IsHaveRoomInfo { get; set; }
        /// <summary>
        /// 房间数据信息
        /// </summary>
        [ProtoMember(4)]
        public CSRoomInfo RoomInfoList { get; set; }
        /// <summary>
        /// 战斗状态
        /// </summary>
        [ProtoMember(5)]
        public int BattleStatus { get; set; }
        /// <summary>
        /// 是否有新手引导
        /// </summary>
        [ProtoMember(6)]
        public byte IsGuide { get; set; }
        /// <summary>
        /// 战斗开始时间
        /// </summary>
        [ProtoMember(7)]
        public uint StartTime { get; set; }
        /// <summary>
        /// 战斗GID
        /// </summary>
        [ProtoMember(8)]
        public ulong BattleGID { get; set; }
        /// <summary>
        /// 是否多人开始战斗
        /// </summary>
        [ProtoMember(9)]
        public byte MultiPlayerBattle { get; set; }
        /// <summary>
        /// 队长ID
        /// </summary>
        [ProtoMember(10)]
        public ulong CaptainPlayerId { get; set; }
        /// <summary>
        /// 进入关卡的玩家数据
        /// </summary>
        [ProtoMember(11)]
        public List<CSLevelPlayerData> PlayerDataList { get; set; } = new List<CSLevelPlayerData>();
        /// <summary>
        /// 章节ID
        /// </summary>
        [ProtoMember(12)]
        public CSChapterInfo Chapter { get; set; }
        /// <summary>
        /// 当前stage
        /// </summary>
        [ProtoMember(13)]
        public int Stage { get; set; }
        [ProtoMember(14)]
        public int MapID { get; set; }
    }
    /// <summary>
    /// 章节信息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class CSChapterInfo : AMessage, IMessage
    {
        public static CSChapterInfo Create(bool autoReturn = true)
        {
            var cSChapterInfo = MessageObjectPool<CSChapterInfo>.Rent();
            cSChapterInfo.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                cSChapterInfo.SetIsPool(false);
            }
            
            return cSChapterInfo;
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
            ChapterID = default;
            Difficult = default;
            MessageObjectPool<CSChapterInfo>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.CSChapterInfo; } 
        /// <summary>
        /// 章节ID
        /// </summary>
        [ProtoMember(1)]
        public int ChapterID { get; set; }
        /// <summary>
        /// 难度
        /// </summary>
        [ProtoMember(2)]
        public int Difficult { get; set; }
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
        /// <summary>
        /// 房间信息
        /// </summary>
        [ProtoMember(1)]
        public CSRoomInfo RoomInfo { get; set; }
        /// <summary>
        /// 服务器帧ID
        /// </summary>
        [ProtoMember(2)]
        public int SveFrameId { get; set; }
        /// <summary>
        /// 帧数量（最多十帧）
        /// </summary>
        [ProtoMember(3)]
        public int FrameCount { get; set; }
        /// <summary>
        /// 帧数据列表
        /// </summary>
        [ProtoMember(4)]
        public List<CSSyncOneFrameData> FrameDataList { get; set; } = new List<CSSyncOneFrameData>();
    }
    /// <summary>
    /// 进入关卡的玩家数据
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class CSLevelPlayerData : AMessage, IDisposable
    {
        public static CSLevelPlayerData Create(bool autoReturn = true)
        {
            var cSLevelPlayerData = MessageObjectPool<CSLevelPlayerData>.Rent();
            cSLevelPlayerData.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                cSLevelPlayerData.SetIsPool(false);
            }
            
            return cSLevelPlayerData;
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
            if (PlayerShowData != null)
            {
                PlayerShowData.Dispose();
                PlayerShowData = null;
            }
            if (PlayerBattleData != null)
            {
                PlayerBattleData.Dispose();
                PlayerBattleData = null;
            }
            MessageObjectPool<CSLevelPlayerData>.Return(this);
        }
        /// <summary>
        /// 局内表现相关数据,与战斗无关
        /// </summary>
        [ProtoMember(1)]
        public CSMiniRoleBaseShowData PlayerShowData { get; set; }
        /// <summary>
        /// 战斗相关数据
        /// </summary>
        [ProtoMember(2)]
        public CSBattlePlayerData PlayerBattleData { get; set; }
    }
    /// <summary>
    /// 标识玩家的基础显示数据
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class CSMiniRoleBaseShowData : AMessage, IDisposable
    {
        public static CSMiniRoleBaseShowData Create(bool autoReturn = true)
        {
            var cSMiniRoleBaseShowData = MessageObjectPool<CSMiniRoleBaseShowData>.Rent();
            cSMiniRoleBaseShowData.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                cSMiniRoleBaseShowData.SetIsPool(false);
            }
            
            return cSMiniRoleBaseShowData;
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
            Uin = default;
            RoleID = default;
            WorldID = default;
            Online = default;
            FightVal = default;
            VIPLevel = default;
            RoleName = default;
            Sex = default;
            Head = default;
            HeadSex = default;
            HeadURL = default;
            HeadFrame = default;
            MessageObjectPool<CSMiniRoleBaseShowData>.Return(this);
        }
        /// <summary>
        /// Uin
        /// </summary>
        [ProtoMember(1)]
        public uint Uin { get; set; }
        /// <summary>
        /// RoleID
        /// </summary>
        [ProtoMember(2)]
        public ulong RoleID { get; set; }
        /// <summary>
        /// 服ID
        /// </summary>
        [ProtoMember(3)]
        public uint WorldID { get; set; }
        /// <summary>
        /// 在线状态
        /// </summary>
        [ProtoMember(4)]
        public byte Online { get; set; }
        /// <summary>
        /// 战力
        /// </summary>
        [ProtoMember(5)]
        public ulong FightVal { get; set; }
        /// <summary>
        /// Vip等级
        /// </summary>
        [ProtoMember(6)]
        public uint VIPLevel { get; set; }
        /// <summary>
        /// 角色名称
        /// </summary>
        [ProtoMember(7)]
        public string RoleName { get; set; }
        /// <summary>
        /// 性别
        /// </summary>
        [ProtoMember(8)]
        public byte Sex { get; set; }
        /// <summary>
        /// 头像
        /// </summary>
        [ProtoMember(9)]
        public uint Head { get; set; }
        /// <summary>
        /// 头像性别
        /// </summary>
        [ProtoMember(10)]
        public byte HeadSex { get; set; }
        /// <summary>
        /// 头像URL
        /// </summary>
        [ProtoMember(11)]
        public string HeadURL { get; set; }
        /// <summary>
        /// 头像框ID
        /// </summary>
        [ProtoMember(12)]
        public uint HeadFrame { get; set; }
    }
    /// <summary>
    /// 开始一场战斗里玩家的数据
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class CSBattlePlayerData : AMessage, IDisposable
    {
        public static CSBattlePlayerData Create(bool autoReturn = true)
        {
            var cSBattlePlayerData = MessageObjectPool<CSBattlePlayerData>.Rent();
            cSBattlePlayerData.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                cSBattlePlayerData.SetIsPool(false);
            }
            
            return cSBattlePlayerData;
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
            if (PlayerBaseData != null)
            {
                PlayerBaseData.Dispose();
                PlayerBaseData = null;
            }
            if (PlayerRunData != null)
            {
                PlayerRunData.Dispose();
                PlayerRunData = null;
            }
            MessageObjectPool<CSBattlePlayerData>.Return(this);
        }
        /// <summary>
        /// RoleID
        /// </summary>
        [ProtoMember(1)]
        public ulong RoleID { get; set; }
        /// <summary>
        /// 玩家外围基础数据
        /// </summary>
        [ProtoMember(2)]
        public CSBattlePlayerBaseData PlayerBaseData { get; set; }
        /// <summary>
        /// 玩家动态数据
        /// </summary>
        [ProtoMember(3)]
        public CSLevelUnitRunData PlayerRunData { get; set; }
    }
    /// <summary>
    /// 玩家动态数据
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class CSLevelUnitRunData : AMessage, IDisposable
    {
        public static CSLevelUnitRunData Create(bool autoReturn = true)
        {
            var cSLevelUnitRunData = MessageObjectPool<CSLevelUnitRunData>.Rent();
            cSLevelUnitRunData.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                cSLevelUnitRunData.SetIsPool(false);
            }
            
            return cSLevelUnitRunData;
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
            Hp = default;
            Level = default;
            Exp = default;
            Gold = default;
            MessageObjectPool<CSLevelUnitRunData>.Return(this);
        }
        /// <summary>
        /// 玩家当前血量
        /// </summary>
        [ProtoMember(1)]
        public int Hp { get; set; }
        /// <summary>
        /// 等级
        /// </summary>
        [ProtoMember(2)]
        public int Level { get; set; }
        /// <summary>
        /// 经验
        /// </summary>
        [ProtoMember(3)]
        public int Exp { get; set; }
        /// <summary>
        /// 金币
        /// </summary>
        [ProtoMember(4)]
        public int Gold { get; set; }
    }
    /// <summary>
    /// 玩家与战斗关联的外围系统数据
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class CSBattlePlayerBaseData : AMessage, IDisposable
    {
        public static CSBattlePlayerBaseData Create(bool autoReturn = true)
        {
            var cSBattlePlayerBaseData = MessageObjectPool<CSBattlePlayerBaseData>.Rent();
            cSBattlePlayerBaseData.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                cSBattlePlayerBaseData.SetIsPool(false);
            }
            
            return cSBattlePlayerBaseData;
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
            PlayerLevel = default;
            BodyType = default;
            FashionID = default;
            WeaponFashionID = default;
            CreateRoleDays = default;
            FailureCount = default;
            if (AttrData != null)
            {
                AttrData.Dispose();
                AttrData = null;
            }
            MessageObjectPool<CSBattlePlayerBaseData>.Return(this);
        }
        /// <summary>
        /// 玩家等级
        /// </summary>
        [ProtoMember(1)]
        public int PlayerLevel { get; set; }
        /// <summary>
        /// 体型
        /// </summary>
        [ProtoMember(2)]
        public byte BodyType { get; set; }
        /// <summary>
        /// 时装ID
        /// </summary>
        [ProtoMember(3)]
        public uint FashionID { get; set; }
        /// <summary>
        /// 武器时装ID
        /// </summary>
        [ProtoMember(4)]
        public uint WeaponFashionID { get; set; }
        /// <summary>
        /// 创角天数
        /// </summary>
        [ProtoMember(5)]
        public int CreateRoleDays { get; set; }
        /// <summary>
        /// 累计失败次数
        /// </summary>
        [ProtoMember(6)]
        public int FailureCount { get; set; }
        /// <summary>
        /// 属性数据
        /// </summary>
        [ProtoMember(7)]
        public CSUnitBattleAttrData AttrData { get; set; }
    }
    /// <summary>
    /// 玩家战斗属性数据
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class CSUnitBattleAttrData : AMessage, IDisposable
    {
        public static CSUnitBattleAttrData Create(bool autoReturn = true)
        {
            var cSUnitBattleAttrData = MessageObjectPool<CSUnitBattleAttrData>.Rent();
            cSUnitBattleAttrData.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                cSUnitBattleAttrData.SetIsPool(false);
            }
            
            return cSUnitBattleAttrData;
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
            Atk = default;
            Hp = default;
            MaxHp = default;
            MoveSpeed = default;
            MessageObjectPool<CSUnitBattleAttrData>.Return(this);
        }
        /// <summary>
        /// 攻击
        /// </summary>
        [ProtoMember(1)]
        public int Atk { get; set; }
        /// <summary>
        /// 生命
        /// </summary>
        [ProtoMember(2)]
        public int Hp { get; set; }
        /// <summary>
        /// 最大生命
        /// </summary>
        [ProtoMember(3)]
        public int MaxHp { get; set; }
        /// <summary>
        /// 移动速度
        /// </summary>
        [ProtoMember(4)]
        public int MoveSpeed { get; set; }
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
    /// 房间玩家信息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class CSRoomPlayerInfo : AMessage, IDisposable
    {
        public static CSRoomPlayerInfo Create(bool autoReturn = true)
        {
            var cSRoomPlayerInfo = MessageObjectPool<CSRoomPlayerInfo>.Rent();
            cSRoomPlayerInfo.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                cSRoomPlayerInfo.SetIsPool(false);
            }
            
            return cSRoomPlayerInfo;
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
            RoleId = default;
            RoleName = default;
            Level = default;
            FightValue = default;
            MessageObjectPool<CSRoomPlayerInfo>.Return(this);
        }
        /// <summary>
        /// 角色ID
        /// </summary>
        [ProtoMember(1)]
        public ulong RoleId { get; set; }
        /// <summary>
        /// 角色名称
        /// </summary>
        [ProtoMember(2)]
        public string RoleName { get; set; }
        /// <summary>
        /// 等级
        /// </summary>
        [ProtoMember(3)]
        public uint Level { get; set; }
        /// <summary>
        /// 战斗力
        /// </summary>
        [ProtoMember(4)]
        public uint FightValue { get; set; }
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
        /// <summary>
        /// 帧ID
        /// </summary>
        [ProtoMember(1)]
        public int FrameId { get; set; }
        /// <summary>
        /// 加速倍率
        /// </summary>
        [ProtoMember(2)]
        public int SpeedUp { get; set; }
        /// <summary>
        /// 玩家数量
        /// </summary>
        [ProtoMember(3)]
        public int PlayerCount { get; set; }
        /// <summary>
        /// 每个玩家的操作数据
        /// </summary>
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
        /// <summary>
        /// 玩家ID
        /// </summary>
        [ProtoMember(1)]
        public int PlayerId { get; set; }
        /// <summary>
        /// 命令数量
        /// </summary>
        [ProtoMember(2)]
        public int FrameCmdCount { get; set; }
        /// <summary>
        /// 命令列表
        /// </summary>
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
        /// <summary>
        /// 命令类型
        /// </summary>
        [ProtoMember(1)]
        public byte Type { get; set; }
        /// <summary>
        /// 命令数据
        /// </summary>
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
    /// 查询功能开放列表
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_QueryFuncOpenListRequest : AMessage, IRequest
    {
        public static C2G_QueryFuncOpenListRequest Create(bool autoReturn = true)
        {
            var c2G_QueryFuncOpenListRequest = MessageObjectPool<C2G_QueryFuncOpenListRequest>.Rent();
            c2G_QueryFuncOpenListRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_QueryFuncOpenListRequest.SetIsPool(false);
            }
            
            return c2G_QueryFuncOpenListRequest;
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
            MessageObjectPool<C2G_QueryFuncOpenListRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_QueryFuncOpenListRequest; } 
        [ProtoIgnore]
        public G2C_QueryFuncOpenListResponse ResponseType { get; set; }
    }
    /// <summary>
    /// 功能开放列表返回
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_QueryFuncOpenListResponse : AMessage, IResponse
    {
        public static G2C_QueryFuncOpenListResponse Create(bool autoReturn = true)
        {
            var g2C_QueryFuncOpenListResponse = MessageObjectPool<G2C_QueryFuncOpenListResponse>.Rent();
            g2C_QueryFuncOpenListResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_QueryFuncOpenListResponse.SetIsPool(false);
            }
            
            return g2C_QueryFuncOpenListResponse;
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
            OpenFuncList = null;
            MessageObjectPool<G2C_QueryFuncOpenListResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_QueryFuncOpenListResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        /// <summary>
        /// 已开放功能ID列表
        /// </summary>
        [ProtoMember(2)]
        public List<int> OpenFuncList { get; set; }
    }
    /// <summary>
    /// 新增功能开放通知
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_FuncOpenNotify : AMessage, IMessage
    {
        public static G2C_FuncOpenNotify Create(bool autoReturn = true)
        {
            var g2C_FuncOpenNotify = MessageObjectPool<G2C_FuncOpenNotify>.Rent();
            g2C_FuncOpenNotify.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_FuncOpenNotify.SetIsPool(false);
            }
            
            return g2C_FuncOpenNotify;
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
            NewOpenFuncList = null;
            MessageObjectPool<G2C_FuncOpenNotify>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_FuncOpenNotify; } 
        /// <summary>
        /// 新增开放的功能ID列表
        /// </summary>
        [ProtoMember(1)]
        public List<int> NewOpenFuncList { get; set; }
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