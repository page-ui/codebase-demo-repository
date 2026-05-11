namespace Page.Ui.Presentation.Auth.GraphQl.Support;

internal static class AuthRetroEmailBuilder
{
    public static string Build(string email, string code, string actionName, string systemLogHeader)
    {
        var encodedEmail      = System.Net.WebUtility.HtmlEncode(email);
        var encodedCode       = System.Net.WebUtility.HtmlEncode(code);
        var encodedAction     = System.Net.WebUtility.HtmlEncode(actionName);
        var encodedHeader     = System.Net.WebUtility.HtmlEncode(systemLogHeader);
        var encodedActionUpper = System.Net.WebUtility.HtmlEncode(actionName.ToUpperInvariant());
        var encodedExpiry     = System.Net.WebUtility.HtmlEncode(ExpiryFor(actionName));
        var timestamp         = DateTime.UtcNow.ToString("MMM d, yyyy");

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />
  <title>{encodedHeader}</title>
</head>
<body style=""margin:0; padding:0;"" bgcolor=""#f4f4f0"">

  <!-- Outer wrapper -->
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0""
         style=""background-color:#f4f4f0;"">
    <tr>
      <td align=""center"" valign=""top"" style=""padding:40px 16px;"">

        <!-- Content column -->
        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0""
               style=""max-width:560px; width:100%;"">

          <!-- Brand row -->
          <tr>
            <td style=""padding-bottom:18px;"">
              <table cellpadding=""0"" cellspacing=""0"" border=""0"">
                <tr>
                  <td valign=""middle"">
                    <span style=""display:inline-block; width:8px; height:8px;
                                  border-radius:50%; background:#22c55e;
                                  margin-right:8px; vertical-align:middle;""></span>
                    <span style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;
                                  font-size:13px; font-weight:600;
                                  color:#1a1a1a; vertical-align:middle;
                                  letter-spacing:-0.2px;"">Page UI</span>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Subject -->
          <tr>
            <td style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;
                        font-size:22px; font-weight:600; color:#111111;
                        letter-spacing:-0.4px; padding-bottom:6px;"">
              {SubjectFor(actionName)}
            </td>
          </tr>

          <!-- Subtext -->
          <tr>
            <td style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;
                        font-size:14px; color:#6b7280; line-height:1.6;
                        padding-bottom:24px;"">
              {SubtextFor(actionName)}
            </td>
          </tr>

          <!-- ╔══ TERMINAL CARD ══╗ -->
          <tr>
            <td style=""background-color:#0d1117;
                        border-radius:14px;
                        border:1px solid rgba(255,255,255,0.07);
                        overflow:hidden;"">

              <!-- Chrome bar -->
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0""
                     style=""border-bottom:1px solid rgba(255,255,255,0.06);"">
                <tr>
                  <td style=""padding:13px 18px;"">
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
                      <tr>
                        <td valign=""middle"">
                          <span style=""display:inline-block; width:10px; height:10px;
                                        border-radius:50%; background:#ff5f57; margin-right:5px;""></span>
                          <span style=""display:inline-block; width:10px; height:10px;
                                        border-radius:50%; background:#febc2e; margin-right:5px;""></span>
                          <span style=""display:inline-block; width:10px; height:10px;
                                        border-radius:50%; background:#28c840;""></span>
                        </td>
                        <td align=""right"" valign=""middle""
                            style=""font-family:'Courier New',Courier,monospace;
                                   font-size:10px; color:rgba(255,255,255,0.2);
                                   letter-spacing:2px;"">
                          SYS&thinsp;/&thinsp;{encodedActionUpper}
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>

              <!-- Terminal body -->
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
                <tr>
                  <td style=""padding:24px 22px;
                              font-family:'Courier New',Courier,monospace;"">

                    <!-- Access lines -->
                    <p style=""margin:0 0 3px; font-size:12px; color:#4ade80;"">
                      &gt;&gt;&gt; SYSTEM ACCESS GRANTED
                    </p>
                    <p style=""margin:0 0 16px; font-size:12px; color:rgba(74,222,128,0.5);"">
                      &gt;&gt;&gt; INITIATING {encodedActionUpper} PROTOCOL...
                    </p>

                    <!-- Divider -->
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0""
                           style=""margin-bottom:16px;"">
                      <tr>
                        <td style=""height:1px; background-color:rgba(255,255,255,0.06);
                                    font-size:0; line-height:0;"">&nbsp;</td>
                      </tr>
                    </table>

                    <!-- User meta -->
                    <table cellpadding=""0"" cellspacing=""0"" border=""0""
                           style=""margin-bottom:8px;"">
                      <tr>
                        <td style=""font-size:11px; color:rgba(255,255,255,0.3);
                                    letter-spacing:1.5px; padding-right:12px;
                                    vertical-align:top; white-space:nowrap;"">USER_ID</td>
                        <td style=""font-size:12px; color:#4ade80;"">
                          {encodedEmail}
                        </td>
                      </tr>
                    </table>
                    <table cellpadding=""0"" cellspacing=""0"" border=""0""
                           style=""margin-bottom:16px;"">
                      <tr>
                        <td style=""font-size:11px; color:rgba(255,255,255,0.3);
                                    letter-spacing:1.5px; padding-right:12px;
                                    vertical-align:top; white-space:nowrap;"">STATUS&nbsp;&nbsp;</td>
                        <td style=""font-size:12px; color:#4ade80;"">
                          {encodedActionUpper}_REQUESTED
                        </td>
                      </tr>
                    </table>

                    <!-- Divider -->
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0""
                           style=""margin-bottom:16px;"">
                      <tr>
                        <td style=""height:1px; background-color:rgba(255,255,255,0.06);
                                    font-size:0; line-height:0;"">&nbsp;</td>
                      </tr>
                    </table>

                    <!-- Code block -->
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0""
                           style=""background-color:#090d12;
                                  border:1px solid rgba(34,197,94,0.18);
                                  border-radius:10px;
                                  margin-bottom:16px;"">
                      <tr>
                        <td align=""center"" style=""padding:18px 22px;"">
                          <p style=""margin:0 0 10px; font-size:9px;
                                     color:rgba(255,255,255,0.2);
                                     letter-spacing:2px; text-transform:uppercase;"">
                            SECRET CODE
                          </p>
                          <p style=""margin:0; font-size:32px; font-weight:700;
                                     color:#f0fdf4; letter-spacing:9px;"">
                            {encodedCode}
                          </p>
                        </td>
                      </tr>
                    </table>

                    <!-- Expiry -->
                    <p style=""margin:0 0 14px; font-size:11px;
                               color:rgba(255,255,255,0.3);"">
                      EXPIRES_IN:&nbsp;<span style=""color:#4ade80;"">{encodedExpiry}</span>
                    </p>

                    <!-- Warning block -->
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0""
                           style=""background-color:rgba(250,204,21,0.04);
                                  border:1px solid rgba(250,204,21,0.10);
                                  border-radius:7px;"">
                      <tr>
                        <td style=""padding:10px 13px;"">
                          <p style=""margin:0 0 2px; font-size:11px;
                                     color:rgba(255,255,255,0.28); line-height:1.6;"">
                            &gt;&gt;&gt; WARNING: IF YOU DID NOT REQUEST THIS,
                            DISREGARD AND CONTACT SYS_ADMIN.
                          </p>
                          <p style=""margin:0; font-size:11px;
                                     color:rgba(255,255,255,0.28);"">
                            &gt;&gt;&gt; END OF LINE.
                          </p>
                        </td>
                      </tr>
                    </table>

                  </td>
                </tr>
              </table>

            </td>
          </tr>
          <!-- ╚══ TERMINAL CARD ══╝ -->

          <!-- Divider -->
          <tr>
            <td style=""height:1px; background-color:#e5e7eb;
                        font-size:0; line-height:0;
                        padding:20px 0 0;"">&nbsp;</td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""padding-top:14px;"">
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
                <tr>
                  <td style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;
                              font-size:12px; color:#9ca3af; line-height:1.5;"">
                    This is an automated message &mdash; keep the code confidential.
                  </td>
                  <td align=""right""
                      style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;
                              font-size:11px; color:#d1d5db; white-space:nowrap;"">
                    {timestamp}
                  </td>
                </tr>
              </table>
            </td>
          </tr>

        </table>
        <!-- /Content column -->

      </td>
    </tr>
  </table>

</body>
</html>";
    }

    private static string SubjectFor(string actionName) => actionName.ToUpperInvariant() switch
    {
        "EMAIL_VERIFICATION" => "Verify your email address",
        "PASSWORD_RESET"     => "Reset your password",
        "ACCOUNT_DELETION"   => "Confirm account deletion",
        "TWO_FACTOR_AUTH"    => "Two-factor authentication code",
        _                    => "Action required on your account"
    };

    private static string SubtextFor(string actionName) => actionName.ToUpperInvariant() switch
    {
        "EMAIL_VERIFICATION" => "A verification code was requested for your account. Enter the code below to confirm your identity.",
        "PASSWORD_RESET"     => "We received a request to reset your password. Use the code below to proceed.",
        "ACCOUNT_DELETION"   => "A deletion request was initiated for your account. Enter the code below to confirm. This action is irreversible.",
        "TWO_FACTOR_AUTH"    => "Use the code below to complete your sign-in.",
        _                    => "Use the code below to complete the requested action on your account."
    };

    private static string ExpiryFor(string actionName) => actionName.ToUpperInvariant() switch
    {
        "EMAIL_VERIFICATION" => "10 MINUTES",
        "PASSWORD_RESET"     => "15 MINUTES",
        "ACCOUNT_DELETION" => "10 MINUTES",
        "TWO_FACTOR_AUTH"    => "10 MINUTES",
        _                    => "10 MINUTES"
    };
}
