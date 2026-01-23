param (
    [string]$emailFrom,
    [string]$emailTo,
    [string]$smtpServer,
    [int]$smtpPort,
    [string]$smtpUser,
    [string]$emailSubject,
    [string]$emailBody,
    [bool]$isBodyHtml = $false
)



 $smtpServer = "smtp.gmail.com"
 $smtpPort = 587

 $mail = New-Object System.Net.Mail.MailMessage
 $mail.From = $emailFrom
 $mail.To.Add($emailTo)
 $mail.Subject = $emailSubject
 $mail.Body = $emailBody
 $mail.IsBodyHtml = $isBodyHtml

 $securePassword = ConvertTo-SecureString $env:GMAIL_PASS -AsPlainText -Force
 $cred = New-Object System.Management.Automation.PSCredential ($smtpUser, $securePassword)
 $smtp = New-Object System.Net.Mail.SmtpClient($smtpServer, $smtpPort)
 $smtp.EnableSsl = $true
 $smtp.Credentials = $cred

 try {
      $smtp.Send($mail)
        Write-Host "✅ Email sent successfully."
 } catch {
       Write-Error "❌ Failed to send email: $_"
       exit 1
  }            
