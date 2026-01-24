using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public record UnitStackDTO(
    UnitTypeEnum Type,
    int Quantity
);
}
