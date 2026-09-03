using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VBotMauiApp.Models;

namespace VBotMauiApp.Services;

/// <summary>
/// Interface hợp nhất các tính năng VBot Phone SDK cho cả Android &amp; iOS
/// </summary>
public interface IVBotPhoneService
{
    /// <summary>
    /// Event phát ra khi có thay đổi trạng thái cuộc gọi (calling, incoming, confirmed, disconnected)
    /// </summary>
    event EventHandler<CallSinkState>? CallStateChanged;

    /// <summary>
    /// Trạng thái cuộc gọi gần nhất
    /// </summary>
    CallSinkState? CurrentCallState { get; }

    /// <summary>
    /// Khởi tạo SDK listener và cấu hình ban đầu
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Kiểm tra người dùng đã kết nối với tổng đài chưa
    /// </summary>
    Task<bool> IsUserConnectedAsync();

    /// <summary>
    /// Tên hiển thị người dùng (extension / display name)
    /// </summary>
    Task<string?> GetUserDisplayNameAsync();

    /// <summary>
    /// Kết nối đến tổng đài VBot bằng JWT Token
    /// </summary>
    Task<string?> ConnectAsync(VBotCallConfig config);

    /// <summary>
    /// Ngắt kết nối tổng đài
    /// </summary>
    Task<bool> DisconnectAsync();

    /// <summary>
    /// Lấy danh sách hotline khả dụng của tài khoản
    /// </summary>
    Task<List<VBotHotline>> GetHotlinesAsync();

    /// <summary>
    /// Bắt đầu cuộc gọi đi
    /// </summary>
    Task<string?> StartCallAsync(string displayName, string phoneNumber, string hotline);

    /// <summary>
    /// Trả lời cuộc gọi đến
    /// </summary>
    Task AnswerAsync();

    /// <summary>
    /// Tắt máy / kết thúc cuộc gọi
    /// </summary>
    Task HangupAsync();

    /// <summary>
    /// Bật / Tắt micro (Mute)
    /// </summary>
    Task MuteAsync();

    /// <summary>
    /// Bật / Tắt loa ngoài (Speaker)
    /// </summary>
    Task SpeakerAsync();

    /// <summary>
    /// Hủy tài nguyên khi thoát ứng dụng
    /// </summary>
    void Dispose();
}
