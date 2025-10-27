using System.Net;
using System.Net.Mail;
using DotNetEnv;
using QRCoder;
using System.Drawing.Imaging;
using e_Vent.dtos;

public class EmailService
{

  public EmailService()
  {
      Env.Load();
  }
  public async Task SendEmailWithQrAsync(string to, string subject, string qrData)
  {
      var smtpServer = Environment.GetEnvironmentVariable("SmtpServer");
      var port = int.Parse(Environment.GetEnvironmentVariable("Port") ?? "587");
      var senderEmail = Environment.GetEnvironmentVariable("SenderEmail") ?? "null";
      var senderName = Environment.GetEnvironmentVariable("SenderName");
      var username = Environment.GetEnvironmentVariable("User");
      var password = Environment.GetEnvironmentVariable("Password");


      using var qrGenerator = new QRCodeGenerator();
      using var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
      using var qrCode = new QRCode(qrCodeData);
      using var qrImage = qrCode.GetGraphic(20);

      // Convert image to memory stream
      using var ms = new MemoryStream();
      qrImage.Save(ms, ImageFormat.Png);
      var qrBytes = ms.ToArray();
      string base64Qr = Convert.ToBase64String(qrBytes);

      string htmlBody = $@"
      <html>
        <body style='font-family:Arial,sans-serif; line-height:1.6; background-color:#f9f9f9; padding:20px;'>
          <div style='max-width:600px; margin:auto; background:white; border-radius:10px; padding:30px; box-shadow:0 2px 5px rgba(0,0,0,0.1);'>
            <h2 style='color:#333;'>🎟️ Your Event QR Code</h2>
            <p>Hello,</p>
            <p>Please find your QR code attached. You can print this QR code and present it at the event gate for verification (If need be).</p>
            <p style='font-size:12px; color:#777;'>If you have any issues scanning, please contact support.</p>
          </div>
        </body>
      </html>";

      var mail = new MailMessage
      {
        From = new MailAddress(senderEmail, senderName),
        Subject = subject,
        Body = htmlBody,
        IsBodyHtml = true
      };
      mail.To.Add(to);

      // 🔹 4️⃣ Attach QR as file (without saving it)
      var qrStream = new MemoryStream(qrBytes);
      qrStream.Position = 0;
      var attachment = new Attachment(qrStream, "EventQRCode.png", "image/png");
      mail.Attachments.Add(attachment);

      // 🔹 5️⃣ Send the email
      using var smtp = new SmtpClient(smtpServer)
      {
        Port = port,
        Credentials = new NetworkCredential(username, password),
        EnableSsl = true
      };

      await smtp.SendMailAsync(mail);
  }

  public async Task SendAnEmailList (Message message, List<string> emails)
  {
    var smtpServer = Environment.GetEnvironmentVariable("SmtpServer");
    var port = int.Parse(Environment.GetEnvironmentVariable("Port") ?? "587");
    var senderEmail = Environment.GetEnvironmentVariable("SenderEmail") ?? "null";
    var senderName = Environment.GetEnvironmentVariable("SenderName");
    var username = Environment.GetEnvironmentVariable("Username");
    var password = Environment.GetEnvironmentVariable("Password");
    var mail = new MailMessage
    {
      From = new MailAddress(senderEmail, senderName),
      Subject = message.Subject,
      Body = message.Body,
      IsBodyHtml = true
    };
    
    foreach (string item in emails)
    {
      mail.To.Add(item);
    }

    using var smtp = new SmtpClient(smtpServer)
    {
      Port = port,
      Credentials = new NetworkCredential(username, password),
      EnableSsl = true
    };
    await smtp.SendMailAsync(mail);
  }
}
