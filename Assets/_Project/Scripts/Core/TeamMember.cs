using UnityEngine;

namespace Deadzone.Core
{
    public class TeamMember : MonoBehaviour
    {
        [Tooltip("0 = Infected/Neutral, 1 = Player/Friendly, 2 = Rival Alpha, 3 = Rival Bravo")]
        [SerializeField] private int teamId = 0;

        public int TeamId 
        { 
            get => teamId; 
            set => teamId = value; 
        }

        public bool IsHostileTo(int otherTeam)
        {
            // Same team is friendly, different team is hostile
            return teamId != otherTeam;
        }

        public bool IsHostileTo(TeamMember other)
        {
            if (other == null) return true;
            return IsHostileTo(other.TeamId);
        }
    }
}
