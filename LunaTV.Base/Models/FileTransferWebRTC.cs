namespace LunaTV.Base.Models;

/// WebRTCOffer WebRTC offer 结构
public class WebRTCOffer
{
    public string SDP { get; set; }
    public string Type { get; set; }
}

/// WebRTCAnswer WebRTC answer 结构
public class WebRTCAnswer
{
    public string SDP { get; set; }
    public string Type { get; set; }
}

/// WebRTCICECandidate ICE candidate 结构
public class WebRTCICECandidate
{
    public string Candidate { get; set; }
    public string SDPMLineIndex { get; set; }
    public string SDPMid { get; set; }
}

/// VideoMessage 视频消息结构
public class VideoMessage
{
    public string Type { get; set; }
    public object Data { get; set; }
}

// ClientInfo 客户端连接信息
public class ClientInfo
{
    public string Id { get; set; } // 客户端唯一标识
    public string Role { get; set; } // sender 或 receiver
    public object Connection { get; set; } // WebSocket连接（不序列化）
    public string JoinedAt { get; set; } // 加入时间
    public string UserAgent { get; set; } // 用户代理
}

// RoomStatus 房间状态信息
public class RoomStatus
{
    public string Code { get; set; }
    public bool SenderOnline { get; set; }
    public bool ReceiverOnline { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ErrorResponse 错误响应结构
public class ErrorResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string Code { get; set; }
}