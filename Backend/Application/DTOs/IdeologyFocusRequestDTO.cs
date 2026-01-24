using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public record IdeologyFocusRequestDTO(IdeologyFocusNameEnum IdeologyFocusName, Guid CityId);
    public record IdeologyFocusAnswerDTO(IdeologyFocusNameEnum? IdeologyFocusName, Guid? CityId, string Message, bool Success);
}
