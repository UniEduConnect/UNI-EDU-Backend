using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.DTOs.Chat;
using UNI_EDU_Backend.Application.Interfaces;

namespace UNI_EDU_Backend.API.Controllers;

// Public support assistant. Uses the AI completion service when configured, otherwise a
// deterministic keyword fallback — no fabricated "live agent" data.
[Route("api/chat")]
[ApiController]
public class ChatBotController(IAiCompletionService ai) : ControllerBase
{
    private readonly IAiCompletionService _ai = ai;

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Chat([FromBody] ChatBotRequest request, CancellationToken cancellationToken)
    {
        var msg = (request?.Message ?? string.Empty).Trim();
        if (msg.Length == 0)
            return StatusCode(StatusCodes.Status200OK, Wrap(new ChatBotReply { Reply = "Mình có thể giúp gì cho bạn? Hãy hỏi về tìm gia sư, đăng ký, học phí hoặc luyện thi nhé!" }));
        if (msg.Length > 500) msg = msg[..500];

        const string system =
            "Bạn là trợ lý AI của UNI EDU — nền tảng kết nối gia sư & luyện thi thông minh. " +
            "Trả lời ngắn gọn (tối đa 3 câu), thân thiện, bằng tiếng Việt. " +
            "Chỉ trả JSON đúng định dạng {\"reply\":\"...\"}.";

        var aiReply = await _ai.CompleteJsonAsync<ChatBotReply>(system, msg, cancellationToken);
        var reply = !string.IsNullOrWhiteSpace(aiReply?.Reply) ? aiReply!.Reply : Fallback(msg);

        return StatusCode(StatusCodes.Status200OK, Wrap(new ChatBotReply { Reply = reply }));
    }

    private static ApiResponse<ChatBotReply> Wrap(ChatBotReply data) => new()
    {
        StatusCode = StatusCodes.Status200OK,
        Message = "OK",
        Data = data,
    };

    private static string Fallback(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("gia sư") || m.Contains("tìm") || m.Contains("tutor"))
            return "Bạn muốn tìm gia sư môn nào? Hãy vào trang \"Tìm gia sư\" để xem hồ sơ, bằng cấp, đánh giá và đặt lịch học thử nhé!";
        if (m.Contains("đăng ký") || m.Contains("dang ky") || m.Contains("tài khoản"))
            return "Để đăng ký, bạn nhấn nút \"Đăng ký\", điền thông tin rồi xác thực email bằng mã OTP là xong. Rất nhanh!";
        if (m.Contains("giá") || m.Contains("phí") || m.Contains("học phí") || m.Contains("tiền"))
            return "Học phí tuỳ môn và từng gia sư, hiển thị rõ trên mỗi hồ sơ. Bạn có thể lọc theo mức giá ngay ở trang \"Tìm gia sư\".";
        if (m.Contains("thi") || m.Contains("luyện thi") || m.Contains("đề"))
            return "UNI EDU có hệ thống luyện thi với đề tạo bằng AI và chấm điểm tự động. Bạn vào mục \"Thi thử online\" để bắt đầu nhé!";
        return "Cảm ơn bạn! Mình sẽ chuyển câu hỏi tới đội ngũ hỗ trợ UNI EDU. Bạn cũng có thể nhắn lại bất cứ lúc nào.";
    }
}
