using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Orderly.Api.Email;

public sealed record EmailMessage(string To, string Subject, string TextBody, string HtmlBody);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct);
}

public sealed class EmailOptions
{
    public const string Section = "Email";

    public string From { get; set; } = "Orderly <no-reply@orderly.local>";
    public SmtpOptions Smtp { get; set; } = new();

    public sealed class SmtpOptions
    {
        /// <summary>Sem Host, os e-mails só vão para o log (modo de desenvolvimento).</summary>
        public string? Host { get; set; }
        public int Port { get; set; } = 587;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool EnableSsl { get; set; } = true;
    }
}

/// <summary>Não envia nada: escreve o e-mail no log. Usado enquanto não há SMTP configurado.</summary>
internal sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        logger.LogWarning(
            "SMTP não configurado; e-mail não enviado. Para: {To} | Assunto: {Subject}\n{Body}",
            message.To,
            message.Subject,
            message.TextBody);
        return Task.CompletedTask;
    }
}

/// <summary>Envio por SMTP (serve para SES, Postmark, Resend, SendGrid etc. via SMTP).</summary>
internal sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        var config = options.Value;
        using var mail = new MailMessage
        {
            From = new MailAddress(config.From),
            Subject = message.Subject,
            Body = message.TextBody,
        };
        mail.To.Add(message.To);
        mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message.HtmlBody, null, "text/html"));

        using var client = new SmtpClient(config.Smtp.Host, config.Smtp.Port)
        {
            EnableSsl = config.Smtp.EnableSsl,
        };
        if (!string.IsNullOrEmpty(config.Smtp.Username))
        {
            client.Credentials = new NetworkCredential(config.Smtp.Username, config.Smtp.Password);
        }

        await client.SendMailAsync(mail, ct);
    }
}
