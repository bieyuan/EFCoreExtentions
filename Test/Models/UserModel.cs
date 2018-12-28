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

        //[FromEntity("Name","Role")]
        //public string RoleName1 { get; set; }

        [FromEntity("Name", "Department", "Role")]
        public string DepartmentName { get; set; }

        //public virtual RoleModel Role { get; set; }

        //[FromEntity("Department", "Role")]
        //public virtual Department Department { get; set; }
    }
}
