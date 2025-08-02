using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task_Management_API.Domain.Models;

namespace Task_Management_Api.Application.DTO
{
    public class AdminSetting
    {
            public string DefaultAdminPassword { get; set; }
            public List<AdminUserSetting> Admins { get; set; }
    }
}
