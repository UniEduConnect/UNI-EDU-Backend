using System.Text;

namespace UNI_EDU_Backend.Application.Services.Otp;

// Builds the OTP verification email — table-based, inline-styled HTML that renders
// reliably across Gmail / Apple Mail / Outlook. The brand mark is pure HTML/CSS
// (a gradient rounded "app icon" + 🎓 + UNI·EDU wordmark) so no external image is needed.
public static class OtpEmailTemplate
{
    public const string Brand = "#1d4ed8";
    private const string BrandLight = "#3b82f6";
    private const string Ink = "#0f172a";
    private const string Muted = "#64748b";

    public static string Subject(string code) => $"{code} là mã xác thực UNI EDU của bạn";

    public static string BuildHtml(string code, int expiresMinutes)
    {
        var digits = BuildDigitBoxes(code);
        var preheader = $"Mã xác thực của bạn là {code}. Hiệu lực trong {expiresMinutes} phút.";

        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<meta name=""color-scheme"" content=""light"">
<title>Mã xác thực UNI EDU</title>
</head>
<body style=""margin:0;padding:0;background:#eef2fb;-webkit-font-smoothing:antialiased;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;"">
  <div style=""display:none;max-height:0;overflow:hidden;opacity:0;color:#eef2fb;font-size:1px;line-height:1px;"">{preheader}</div>

  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#eef2fb;padding:32px 12px;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" width=""600"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;width:100%;background:#ffffff;border-radius:20px;overflow:hidden;box-shadow:0 18px 48px rgba(29,78,216,0.16);"">

          <!-- Header / brand band -->
          <tr>
            <td style=""background:{Brand};background:linear-gradient(135deg,{Brand} 0%,{BrandLight} 100%);padding:34px 40px;"">
              <table role=""presentation"" cellpadding=""0"" cellspacing=""0"">
                <tr>
                  <td style=""vertical-align:middle;"">
                    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"">
                      <tr>
                        <td width=""54"" height=""54"" align=""center"" valign=""middle"" style=""width:54px;height:54px;background:#ffffff;background:rgba(255,255,255,0.16);border-radius:16px;font-size:28px;line-height:54px;text-align:center;"">🎓</td>
                        <td style=""padding-left:14px;vertical-align:middle;"">
                          <div style=""font-size:22px;font-weight:800;color:#ffffff;letter-spacing:0.5px;line-height:1;"">UNI<span style=""color:#bfdbfe;"">·</span>EDU</div>
                          <div style=""font-size:12px;color:#dbeafe;margin-top:4px;letter-spacing:0.4px;"">Nền tảng gia sư &amp; luyện thi</div>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Body -->
          <tr>
            <td style=""padding:40px 40px 8px 40px;"">
              <h1 style=""margin:0 0 8px 0;font-size:23px;font-weight:800;color:{Ink};"">Xác thực email của bạn 👋</h1>
              <p style=""margin:0;font-size:15px;line-height:24px;color:{Muted};"">
                Cảm ơn bạn đã đăng ký <strong style=""color:{Ink};"">UNI EDU</strong>. Nhập mã xác thực bên dưới để hoàn tất đăng ký tài khoản.
              </p>
            </td>
          </tr>

          <!-- OTP digit boxes -->
          <tr>
            <td style=""padding:26px 40px 6px 40px;"">
              <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" align=""center"" style=""margin:0 auto;"">
                <tr>{digits}</tr>
              </table>
            </td>
          </tr>

          <!-- Expiry pill -->
          <tr>
            <td align=""center"" style=""padding:6px 40px 8px 40px;"">
              <span style=""display:inline-block;background:#eff6ff;color:{Brand};font-size:13px;font-weight:600;padding:8px 16px;border-radius:999px;"">
                ⏱️ Mã có hiệu lực trong {expiresMinutes} phút
              </span>
            </td>
          </tr>

          <!-- Security note -->
          <tr>
            <td style=""padding:18px 40px 8px 40px;"">
              <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#fffbeb;border:1px solid #fde68a;border-radius:14px;"">
                <tr>
                  <td style=""padding:14px 16px;font-size:13px;line-height:20px;color:#92400e;"">
                    🔒 <strong>Bảo mật:</strong> UNI EDU sẽ không bao giờ hỏi mã này qua điện thoại hay tin nhắn. Tuyệt đối <strong>không chia sẻ</strong> mã cho bất kỳ ai.
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Divider -->
          <tr><td style=""padding:18px 40px 0 40px;""><div style=""height:1px;background:#e2e8f0;""></div></td></tr>

          <!-- Footer -->
          <tr>
            <td style=""padding:18px 40px 36px 40px;"">
              <p style=""margin:0 0 6px 0;font-size:12.5px;line-height:20px;color:{Muted};"">
                Bạn không yêu cầu đăng ký? Hãy bỏ qua email này — không có thay đổi nào với tài khoản của bạn.
              </p>
              <p style=""margin:0;font-size:12px;color:#94a3b8;"">© {{0}} UNI EDU · Đồng hành cùng hành trình học tập của bạn.</p>
            </td>
          </tr>

        </table>

        <div style=""font-size:11px;color:#94a3b8;margin-top:18px;"">Email tự động — vui lòng không trả lời.</div>
      </td>
    </tr>
  </table>
</body>
</html>".Replace("{0}", DateTime.UtcNow.Year.ToString());
    }

    private static string BuildDigitBoxes(string code)
    {
        var sb = new StringBuilder();
        foreach (var ch in code)
        {
            sb.Append($@"<td style=""padding:0 5px;"">
              <div style=""width:46px;height:58px;line-height:58px;text-align:center;font-size:28px;font-weight:800;color:{Brand};background:#f1f5ff;border:2px solid #dbe4ff;border-radius:12px;font-family:'Segoe UI',Roboto,monospace;"">{ch}</div>
            </td>");
        }
        return sb.ToString();
    }

    // Plain-text fallback for clients that don't render HTML.
    public static string BuildText(string code, int expiresMinutes) =>
        $"UNI EDU - Xac thuc email\n\nMa xac thuc cua ban la: {code}\nMa co hieu luc trong {expiresMinutes} phut.\nKhong chia se ma nay cho bat ky ai.\n\nNeu ban khong yeu cau, hay bo qua email nay.";
}
