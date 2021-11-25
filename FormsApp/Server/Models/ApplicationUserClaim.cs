using Microsoft.AspNetCore.Identity;

namespace FormsApp.Server.Models
{
    public class ApplicationUserClaim : IdentityUserClaim<string>
    {
        //public ApplicationUserClaim(string userId, string claimType,
        //string claimValue, ActiveStatus status, string departmentInfo)
        //{
        //    UserId = userId;
        //    ClaimType = claimType;
        //    ClaimValue = claimValue;
        //    Status = status;
        //    DepartmentInfo = departmentInfo;
        //}
        //public virtual ApplicationUser User { get; set; }
        public ActiveStatus Status { get; set; }
        public string? DepartmentInfo { get; set; }
    }

    public enum ActiveStatus
    {
        Active,
        Suspended,
        OnLeave,
        InActive,
        Deceased
    }
}