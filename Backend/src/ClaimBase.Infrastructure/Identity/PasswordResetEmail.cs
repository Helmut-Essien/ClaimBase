using System.Net;
using ClaimBase.Application.Common;

namespace ClaimBase.Infrastructure.Identity;

/// <summary>Reset-email body. The link, the one-hour expiry, and the "ignore this email" line stay together.</summary>
public static class PasswordResetEmail
{
    private const string BrandPrimary = "#512BD4";
    private const string BrandPrimaryDark = "#2B0B98";
    private const string TextColor = "#333333";
    private const string MutedColor = "#666666";
    private const string Page = "#F8F9FA";

    /// <summary>Subject line.</summary>
    public const string Subject = "Reset your ClaimBase password";

    /// <summary>
    /// Builds the plain and HTML bodies. The URL is encoded in HTML and left raw in the plain part.
    /// </summary>
    /// <param name="resetUrl">Absolute reset link.</param>
    /// <returns>Plain text and HTML.</returns>
    public static (string Plain, string Html) Compose(string resetUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resetUrl);

        var safeUrl = WebUtility.HtmlEncode(resetUrl);
        var expiry = WebUtility.HtmlEncode(PasswordResetLifetime.ExpiryLabel);
        var plain = $"""
            Reset your ClaimBase password

            We received a request to reset your password.

            Reset your password:
            {resetUrl}

            This link expires in {PasswordResetLifetime.ExpiryLabel}.

            If you did not request a password reset, you can ignore this email.

            We will never ask for your password by email.
            """;

        var html = $"""
            <div style="margin:0;padding:24px;background:{Page};font-family:'Open Sans',Arial,sans-serif;color:{TextColor};">
              <div style="max-width:560px;margin:0 auto;background:#ffffff;border-radius:12px;border:1px solid #eeeeee;border-left:4px solid #005A9C;padding:24px;">
                <h1 style="margin:0 0 16px;font-size:20px;line-height:1.3;">Reset your password</h1>
                <p style="margin:0 0 16px;">We received a request to reset your ClaimBase password.</p>
                <p style="margin:0 0 8px;">Use the button below to choose a new password:</p>
                <p style="margin:16px 0;">
                  <a href="{safeUrl}" style="display:inline-block;padding:12px 20px;border-radius:20px;background:{BrandPrimary};background-image:linear-gradient(to right,{BrandPrimary},{BrandPrimaryDark});color:#ffffff;font-weight:700;text-decoration:none;">Reset password</a>
                </p>
                <p style="margin:0 0 16px;padding:12px;background:#f3eefe;border-radius:12px;">This link expires in {expiry}.</p>
                <p style="margin:0 0 16px;font-size:14px;color:{MutedColor};">If the button does not work, open this link: <a href="{safeUrl}" style="color:#005A9C;">{safeUrl}</a></p>
                <p style="margin:0 0 8px;">If you did not request a password reset, you can ignore this email.</p>
                <p style="margin:0;">We will never ask for your password by email.</p>
              </div>
            </div>
            """;

        return (plain, html);
    }
}
