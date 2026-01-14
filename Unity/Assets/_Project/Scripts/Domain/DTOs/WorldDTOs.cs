using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public class WorldAvailableResponseDTO
{
    public string WorldId;
    public string WorldName;
    public int CurrentPlayerCount;
    public int MaxPlayerCapacity;
    public bool IsCurrentPlayerMember;
}