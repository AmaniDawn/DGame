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
    /// <summary>
    /// Gate 请求 Game 创建房间
    /// </summary>
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
        /// <summary>
        /// 角色ID
        /// </summary>
        [ProtoMember(1)]
        public long RoleId { get; set; }
        /// <summary>
        /// 账号ID
        /// </summary>
        [ProtoMember(2)]
        public long AccountId { get; set; }
        /// <summary>
        /// 服务器ID
        /// </summary>
        [ProtoMember(3)]
        public int ServerId { get; set; }
        /// <summary>
        /// Session运行时ID
        /// </summary>
        [ProtoMember(4)]
        public long SessionRuntimeId { get; set; }
        /// <summary>
        /// 角色名称
        /// </summary>
        [ProtoMember(5)]
        public string RoleName { get; set; }
        /// <summary>
        /// 头像ID
        /// </summary>
        [ProtoMember(6)]
        public int HeadId { get; set; }
        /// <summary>
        /// 性别
        /// </summary>
        [ProtoMember(7)]
        public byte Sex { get; set; }
        /// <summary>
        /// 等级
        /// </summary>
        [ProtoMember(8)]
        public uint Level { get; set; }
        /// <summary>
        /// 战斗力
        /// </summary>
        [ProtoMember(9)]
        public uint FightValue { get; set; }
        /// <summary>
        /// 房间最大玩家数量
        /// </summary>
        [ProtoMember(10)]
        public int PlayerCount { get; set; }
    }
    /// <summary>
    /// Game 创建房间返回
    /// </summary>
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
            MessageObjectPool<G2Game_CreateRoomResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Game_CreateRoomResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        /// <summary>
        /// 房间ID
        /// </summary>
        [ProtoMember(2)]
        public int RoomId { get; set; }
        /// <summary>
        /// 房间序号
        /// </summary>
        [ProtoMember(3)]
        public int RoomSeq { get; set; }
        /// <summary>
        /// 当前房间玩家数量
        /// </summary>
        [ProtoMember(4)]
        public int PlayerCount { get; set; }
        /// <summary>
        /// 当前房间玩家列表
        /// </summary>
        [ProtoMember(5)]
        public List<InnerRoomPlayerInfo> PlayerInfos { get; set; } = new List<InnerRoomPlayerInfo>();
    }
    /// <summary>
    /// Gate 请求 Game 加入房间
    /// </summary>
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
        /// <summary>
        /// 房间ID
        /// </summary>
        [ProtoMember(1)]
        public int RoomId { get; set; }
        /// <summary>
        /// 角色ID
        /// </summary>
        [ProtoMember(2)]
        public long RoleId { get; set; }
        /// <summary>
        /// 账号ID
        /// </summary>
        [ProtoMember(3)]
        public long AccountId { get; set; }
        /// <summary>
        /// 服务器ID
        /// </summary>
        [ProtoMember(4)]
        public int ServerId { get; set; }
        /// <summary>
        /// Session运行时ID
        /// </summary>
        [ProtoMember(5)]
        public long SessionRuntimeId { get; set; }
        /// <summary>
        /// 角色名称
        /// </summary>
        [ProtoMember(6)]
        public string RoleName { get; set; }
        /// <summary>
        /// 头像ID
        /// </summary>
        [ProtoMember(7)]
        public int HeadId { get; set; }
        /// <summary>
        /// 性别
        /// </summary>
        [ProtoMember(8)]
        public byte Sex { get; set; }
        /// <summary>
        /// 等级
        /// </summary>
        [ProtoMember(9)]
        public uint Level { get; set; }
        /// <summary>
        /// 战斗力
        /// </summary>
        [ProtoMember(10)]
        public uint FightValue { get; set; }
    }
    /// <summary>
    /// Game 加入房间返回
    /// </summary>
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
            MessageObjectPool<G2Game_JoinRoomResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Game_JoinRoomResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        /// <summary>
        /// 房间ID
        /// </summary>
        [ProtoMember(2)]
        public int RoomId { get; set; }
        /// <summary>
        /// 房间序号
        /// </summary>
        [ProtoMember(3)]
        public int RoomSeq { get; set; }
        /// <summary>
        /// 当前房间玩家数量
        /// </summary>
        [ProtoMember(4)]
        public int PlayerCount { get; set; }
        /// <summary>
        /// 当前房间玩家列表
        /// </summary>
        [ProtoMember(5)]
        public List<InnerRoomPlayerInfo> PlayerInfos { get; set; } = new List<InnerRoomPlayerInfo>();
    }
    /// <summary>
    /// Gate 请求 Game 离开房间
    /// </summary>
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
        /// <summary>
        /// 房间ID
        /// </summary>
        [ProtoMember(1)]
        public int RoomId { get; set; }
        /// <summary>
        /// 角色ID
        /// </summary>
        [ProtoMember(2)]
        public long RoleId { get; set; }
    }
    /// <summary>
    /// Game 离开房间返回
    /// </summary>
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
    /// <summary>
    /// 房间玩家快照
    /// </summary>
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
        /// <summary>
        /// 角色ID
        /// </summary>
        [ProtoMember(1)]
        public long RoleId { get; set; }
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
}