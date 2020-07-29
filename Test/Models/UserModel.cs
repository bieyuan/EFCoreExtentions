using EFCoreExtentions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Test
{
    public partial class UserModel
    {
        public string Name { get; set; }

        public string RoleName { get; set; }

        //[FromEntity("Role.Name")]
        //public string RoleName1 { get; set; }

        [FromEntity("Role.Department.Name")]
        public string DepartmentName { get; set; }

        //public virtual RoleModel Role { get; set; }

        //[FromEntity("Department", "Role")]
        //public virtual Department Department { get; set; }
    }
}
