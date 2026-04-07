namespace SV22T1020232.Shop.Models
{
    public class ChangePasswordInput
    {
        public string OldPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
    }
}
