using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantSystem.Domain.Common.Base
{
    public abstract class Entity : BaseEntity
    {
        public Guid Id { get; set; }
    }
}
