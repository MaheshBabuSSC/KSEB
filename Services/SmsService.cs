namespace KSEB.Services
{
    public class SmsService
    {
        public void SendOtp(string phoneNumber, string otp)
        {
            // TEMP: Replace with Twilio / MSG91 / AWS SNS
            Console.WriteLine($"OTP sent to {phoneNumber}: {otp}");
        }
    }
}
