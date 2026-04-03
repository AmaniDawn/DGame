using LightProto;
using MemoryPack;
using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Fantasy;
using Fantasy.Pool;
using Fantasy.Network.Interface;
using Fantasy.Serialize;

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
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618
namespace Fantasy
{
    [Serializable]
    [ProtoContract]
    public partial class G2Game_CreateRoomRequest : AMessage, IAddressRequest
    {
        public static G2Game_CreateRoomRequest Create(bool autoReturn = true)
        {
            var g2Game_CreateRoomRequest = MessageObjectPool<G2Game_CreateRoomRequest>.Rent();
            g2Game_CreateRoomRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2Game_CreateRoomRequest.SetIsPool(false);
            }
            
            return g2Game_CreateRoomRequest;
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
            AccountId = default;
            ServerId = default;
            SessionRuntimeId = default;
            RoleName = default;
            HeadId = default;
            Sex = default;
            Level = default;
            FightValue = default;
            PlayerCount = default;
            MessageObjectPool<G2Game_CreateRoomRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Game_CreateRoomRequest; } 
        [ProtoIgnore]
        public G2Game_CreateRoomResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public long RoleId { get; set; }
        [ProtoMember(2)]
        public long AccountId { get; set; }
        [ProtoMember(3)]
        public int ServerId { get; set; }
        [ProtoMember(4)]
        public long SessionRuntimeId { get; set; }
        [ProtoMember(5)]
        public string RoleName { get; set; }
        [ProtoMember(6)]
        public int HeadId { get; set; }
        [ProtoMember(7)]
        public byte Sex { get; set; }
        [ProtoMember(8)]
        public uint Level { get; set; }
        [ProtoMember(9)]
        public uint FightValue { get; set; }
        [ProtoMember(10)]
        public int PlayerCount { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2Game_CreateRoomResponse : AMessage, IAddressResponse
    {
        public static G2Game_CreateRoomResponse Create(bool autoReturn = true)
        {
            var g2Game_CreateRoomResponse = MessageObjectPool<G2Game_CreateRoomResponse>.Rent();
            g2Game_CreateRoomResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2Game_CreateRoomResponse.SetIsPool(false);
            }
            
            return g2Game_CreateRoomResponse;
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
            RoomId = default;
            RoomSeq = default;
            PlayerCount = default;
            PlayerInfos.Clear();
            CaptainRoleId = default;
            MessageObjectPool<G2Game_CreateRoomResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Game_CreateRoomResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public int RoomId { get; set; }
        [ProtoMember(3)]
        public int RoomSeq { get; set; }
        [ProtoMember(4)]
        public int PlayerCount { get; set; }
        [ProtoMember(5)]
        public List<InnerRoomPlayerInfo> PlayerInfos { get; set; } = new List<InnerRoomPlayerInfo>();
        [ProtoMember(6)]
        public long CaptainRoleId { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2Game_JoinRoomRequest : AMessage, IAddressRequest
    {
        public static G2Game_JoinRoomRequest Create(bool autoReturn = true)
        {
            var g2Game_JoinRoomRequest = MessageObjectPool<G2Game_JoinRoomRequest>.Rent();
            g2Game_JoinRoomRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2Game_JoinRoomRequest.SetIsPool(false);
            }
            
            return g2Game_JoinRoomRequest;
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
            RoleId = default;
            AccountId = default;
            ServerId = default;
            SessionRuntimeId = default;
            RoleName = default;
            HeadId = default;
            Sex = default;
            Level = default;
            FightValue = default;
            MessageObjectPool<G2Game_JoinRoomRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Game_JoinRoomRequest; } 
        [ProtoIgnore]
        public G2Game_JoinRoomResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public int RoomId { get; set; }
        [ProtoMember(2)]
        public long RoleId { get; set; }
        [ProtoMember(3)]
        public long AccountId { get; set; }
        [ProtoMember(4)]
        public int ServerId { get; set; }
        [ProtoMember(5)]
        public long SessionRuntimeId { get; set; }
        [ProtoMember(6)]
        public string RoleName { get; set; }
        [ProtoMember(7)]
        public int HeadId { get; set; }
        [ProtoMember(8)]
        public byte Sex { get; set; }
        [ProtoMember(9)]
        public uint Level { get; set; }
        [ProtoMember(10)]
        public uint FightValue { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2Game_JoinRoomResponse : AMessage, IAddressResponse
    {
        public static G2Game_JoinRoomResponse Create(bool autoReturn = true)
        {
            var g2Game_JoinRoomResponse = MessageObjectPool<G2Game_JoinRoomResponse>.Rent();
            g2Game_JoinRoomResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2Game_JoinRoomResponse.SetIsPool(false);
            }
            
            return g2Game_JoinRoomResponse;
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
            RoomId = default;
            RoomSeq = default;
            PlayerCount = default;
            PlayerInfos.Clear();
            CaptainRoleId = default;
            MessageObjectPool<G2Game_JoinRoomResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Game_JoinRoomResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public int RoomId { get; set; }
        [ProtoMember(3)]
        public int RoomSeq { get; set; }
        [ProtoMember(4)]
        public int PlayerCount { get; set; }
        [ProtoMember(5)]
        public List<InnerRoomPlayerInfo> PlayerInfos { get; set; } = new List<InnerRoomPlayerInfo>();
        [ProtoMember(6)]
        public long CaptainRoleId { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2Game_LeaveRoomRequest : AMessage, IAddressRequest
    {
        public static G2Game_LeaveRoomRequest Create(bool autoReturn = true)
        {
            var g2Game_LeaveRoomRequest = MessageObjectPool<G2Game_LeaveRoomRequest>.Rent();
            g2Game_LeaveRoomRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2Game_LeaveRoomRequest.SetIsPool(false);
            }
            
            return g2Game_LeaveRoomRequest;
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
            RoleId = default;
            MessageObjectPool<G2Game_LeaveRoomRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Game_LeaveRoomRequest; } 
        [ProtoIgnore]
        public G2Game_LeaveRoomResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public int RoomId { get; set; }
        [ProtoMember(2)]
        public long RoleId { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2Game_LeaveRoomResponse : AMessage, IAddressResponse
    {
        public static G2Game_LeaveRoomResponse Create(bool autoReturn = true)
        {
            var g2Game_LeaveRoomResponse = MessageObjectPool<G2Game_LeaveRoomResponse>.Rent();
            g2Game_LeaveRoomResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2Game_LeaveRoomResponse.SetIsPool(false);
            }
            
            return g2Game_LeaveRoomResponse;
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
            MessageObjectPool<G2Game_LeaveRoomResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Game_LeaveRoomResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2Game_StartBattleRequest : AMessage, IAddressRequest
    {
        public static G2Game_StartBattleRequest Create(bool autoReturn = true)
        {
            var g2Game_StartBattleRequest = MessageObjectPool<G2Game_StartBattleRequest>.Rent();
            g2Game_StartBattleRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2Game_StartBattleRequest.SetIsPool(false);
            }
            
            return g2Game_StartBattleRequest;
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
            RoleId = default;
            MessageObjectPool<G2Game_StartBattleRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Game_StartBattleRequest; } 
        [ProtoIgnore]
        public G2Game_StartBattleResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public int RoomId { get; set; }
        [ProtoMember(2)]
        public long RoleId { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2Game_BattleLoadDoneRequest : AMessage, IAddressRequest
    {
        public static G2Game_BattleLoadDoneRequest Create(bool autoReturn = true)
        {
            var g2Game_BattleLoadDoneRequest = MessageObjectPool<G2Game_BattleLoadDoneRequest>.Rent();
            g2Game_BattleLoadDoneRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2Game_BattleLoadDoneRequest.SetIsPool(false);
            }
            
            return g2Game_BattleLoadDoneRequest;
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
            RoleId = default;
            MessageObjectPool<G2Game_BattleLoadDoneRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Game_BattleLoadDoneRequest; } 
        [ProtoIgnore]
        public G2Game_StartBattleResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public int RoomId { get; set; }
        [ProtoMember(2)]
        public long RoleId { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2Game_StartBattleResponse : AMessage, IAddressResponse
    {
        public static G2Game_StartBattleResponse Create(bool autoReturn = true)
        {
            var g2Game_StartBattleResponse = MessageObjectPool<G2Game_StartBattleResponse>.Rent();
            g2Game_StartBattleResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2Game_StartBattleResponse.SetIsPool(false);
            }
            
            return g2Game_StartBattleResponse;
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
            SessionRuntimeIds.Clear();
            RoomId = default;
            RoomSeq = default;
            RandSeed = default;
            PlayerCount = default;
            BattleStatus = default;
            IsGuide = default;
            StartTime = default;
            BattleGID = default;
            MultiPlayerBattle = default;
            CaptainPlayerId = default;
            PlayerDataList.Clear();
            Chapter = default;
            Stage = default;
            MapID = default;
            MessageObjectPool<G2Game_StartBattleResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Game_StartBattleResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public List<long> SessionRuntimeIds { get; set; } = new List<long>();
        [ProtoMember(3)]
        public int RoomId { get; set; }
        [ProtoMember(4)]
        public int RoomSeq { get; set; }
        [ProtoMember(5)]
        public int RandSeed { get; set; }
        [ProtoMember(6)]
        public int PlayerCount { get; set; }
        [ProtoMember(7)]
        public int BattleStatus { get; set; }
        [ProtoMember(8)]
        public byte IsGuide { get; set; }
        [ProtoMember(9)]
        public uint StartTime { get; set; }
        [ProtoMember(10)]
        public ulong BattleGID { get; set; }
        [ProtoMember(11)]
        public byte MultiPlayerBattle { get; set; }
        [ProtoMember(12)]
        public ulong CaptainPlayerId { get; set; }
        [ProtoMember(13)]
        public List<CSLevelPlayerData> PlayerDataList { get; set; } = new List<CSLevelPlayerData>();
        [ProtoMember(14)]
        public CSChapterInfo Chapter { get; set; }
        [ProtoMember(15)]
        public int Stage { get; set; }
        [ProtoMember(16)]
        public int MapID { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2Gate_RoomPlayerInfoChangedMessage : AMessage, IAddressMessage
    {
        public static G2Gate_RoomPlayerInfoChangedMessage Create(bool autoReturn = true)
        {
            var g2Gate_RoomPlayerInfoChangedMessage = MessageObjectPool<G2Gate_RoomPlayerInfoChangedMessage>.Rent();
            g2Gate_RoomPlayerInfoChangedMessage.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2Gate_RoomPlayerInfoChangedMessage.SetIsPool(false);
            }
            
            return g2Gate_RoomPlayerInfoChangedMessage;
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
            SessionRuntimeIds.Clear();
            RoomId = default;
            RoomSeq = default;
            PlayerCount = default;
            PlayerInfos.Clear();
            CaptainRoleId = default;
            MessageObjectPool<G2Gate_RoomPlayerInfoChangedMessage>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Gate_RoomPlayerInfoChangedMessage; } 
        [ProtoMember(1)]
        public List<long> SessionRuntimeIds { get; set; } = new List<long>();
        [ProtoMember(2)]
        public int RoomId { get; set; }
        [ProtoMember(3)]
        public int RoomSeq { get; set; }
        [ProtoMember(4)]
        public int PlayerCount { get; set; }
        [ProtoMember(5)]
        public List<InnerRoomPlayerInfo> PlayerInfos { get; set; } = new List<InnerRoomPlayerInfo>();
        [ProtoMember(6)]
        public long CaptainRoleId { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class InnerRoomPlayerInfo : AMessage, IDisposable
    {
        public static InnerRoomPlayerInfo Create(bool autoReturn = true)
        {
            var innerRoomPlayerInfo = MessageObjectPool<InnerRoomPlayerInfo>.Rent();
            innerRoomPlayerInfo.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                innerRoomPlayerInfo.SetIsPool(false);
            }
            
            return innerRoomPlayerInfo;
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
            MessageObjectPool<InnerRoomPlayerInfo>.Return(this);
        }
        [ProtoMember(1)]
        public long RoleId { get; set; }
        [ProtoMember(2)]
        public string RoleName { get; set; }
        [ProtoMember(3)]
        public uint Level { get; set; }
        [ProtoMember(4)]
        public uint FightValue { get; set; }
    }
}