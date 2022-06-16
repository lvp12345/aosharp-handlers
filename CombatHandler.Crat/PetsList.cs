using AOSharp.Common.GameData;
using System.Collections.Generic;
using static CombatHandler.Generic.GenericCombatHandler;

namespace CombatHandler.Crat
{
    public class PetsList
    {
        public static readonly Dictionary<int, PetSpellData> Pets = new Dictionary<int, PetSpellData>
        {
                { 273300, new PetSpellData(273301, 273301, PetType.Attack) }, // CEO Guardian
                { 235386, new PetSpellData(239828, 239828, PetType.Attack) }, // Corporate Guardian
                { 46391, new PetSpellData(96213, 96213, PetType.Attack) }, // Director-Grade Bodyguard
                //added @BeyondDezign
                { 46381, new PetSpellData(96213, 150737, PetType.Attack) }, // Executive-Grade Bodyguard
                { 46356, new PetSpellData(150737, 150738, PetType.Attack) }, // Supervisor-Grade Bodyguard
                { 46410, new PetSpellData(150737, 150738, PetType.Attack) }, // Advanced Bodyguard
                { 46373, new PetSpellData(150738, 150739, PetType.Attack) }, // Faithful Bodyguard
                { 46368, new PetSpellData(150739, 150740, PetType.Attack) }, // Limited Bodyguard
                { 46404, new PetSpellData(150740, 150741, PetType.Attack) }, // Basic Bodyguard
                { 46393, new PetSpellData(150729, 150729, PetType.Attack) }, // Director-Grade Minion
                { 46383, new PetSpellData(150729, 150730, PetType.Attack) }, // Executive-Grade Minion
                { 46358, new PetSpellData(150729, 150730, PetType.Attack) }, // Supervisor-Grade Minion
                { 46412, new PetSpellData(150730, 150731, PetType.Attack) }, // Advanced Minion
                { 46375, new PetSpellData(150730, 150731, PetType.Attack) }, // Faithful Minion
                { 46360, new PetSpellData(150731, 150731, PetType.Attack) }, // Limited Minion
                { 46395, new PetSpellData(150731, 150732, PetType.Attack) }, // Basic Minion
                { 46387, new PetSpellData(150756, 150757, PetType.Attack) }, // Director-Grade Administrator-Droid
                { 46377, new PetSpellData(150757, 150758, PetType.Attack) }, // Executive-Grade Administrator-Droid
                { 46352, new PetSpellData(150757, 150758, PetType.Attack) }, // Supervisor-Grade Administrator-Droid
                { 46406, new PetSpellData(150758, 150758, PetType.Attack) }, // Advanced Administrator-Droid
                { 46369, new PetSpellData(150758, 150759, PetType.Attack) }, // Faithful Administrator-Droid
                { 46364, new PetSpellData(150758, 150759, PetType.Attack) }, // Limited Administrator-Droid
                { 46400, new PetSpellData(150759, 150760, PetType.Attack) }, // Basic Administrator
                { 46394, new PetSpellData(150723, 150724, PetType.Attack) }, // Director-Grade Secretary-Droid
                { 46384, new PetSpellData(150724, 150724, PetType.Attack) }, // Executive-Grade Secretary-Droid
                { 46350, new PetSpellData(150724, 150725, PetType.Attack) }, // Supervisor-Grade Secretary-Droid
                { 46398, new PetSpellData(150725, 150726, PetType.Attack) }, // Advanced Secretary-Droid
                { 46376, new PetSpellData(150726, 150727, PetType.Attack) }, // Faithful Secretary-Droid
                { 46361, new PetSpellData(150727, 150728, PetType.Attack) }, // Limited Secretary-Droid
                { 46396, new PetSpellData(150728, 150728, PetType.Attack) }, // Basic Secretary-Droid
                { 46388, new PetSpellData(150750, 150751, PetType.Attack) }, // Director-Grade Aide-Droid
                { 46378, new PetSpellData(150751, 150752, PetType.Attack) }, // Executive-Grade Aide-Droid
                { 46353, new PetSpellData(150752, 150753, PetType.Attack) }, // Supervisor-Grade Aide-Droid
                { 46407, new PetSpellData(150752, 150753, PetType.Attack) }, // Advanced Aide-Droid
                { 46370, new PetSpellData(150753, 150754, PetType.Attack) }, // Faithful Aide-Droid
                { 46365, new PetSpellData(150754, 150754, PetType.Attack) }, // Limited Aide-Droid
                { 46401, new PetSpellData(150754, 150755, PetType.Attack) }, // Basic Aide-Droid
                { 46389, new PetSpellData(150745, 150746, PetType.Attack) }, // Director-Grade Assistant-Droid
                { 46379, new PetSpellData(150745, 150746, PetType.Attack) }, // Executive-Grade Assistant-Droid
                { 46354, new PetSpellData(150746, 150747, PetType.Attack) }, // Supervisor-Grade Assistant-Droid *
                { 46408, new PetSpellData(150747, 150747, PetType.Attack) }, // Advanced Assistant-Droid
                { 46371, new PetSpellData(150747, 150748, PetType.Attack) }, // Faithful Assistant-Droid
                { 46366, new PetSpellData(150748, 150749, PetType.Attack) }, // Limited Assistant-Droid
                { 46402, new PetSpellData(150748, 150749, PetType.Attack) }, // Basic Assistant-Droid
                { 46390, new PetSpellData(96201, 150742, PetType.Attack) }, // Director-Grade Attendant-Droid
                { 46380, new PetSpellData(150742, 150742, PetType.Attack) }, // Executive-Grade Attendant-Droid
                { 46355, new PetSpellData(150742, 150743, PetType.Attack) }, // Supervisor-Grade Attendant-Droid 
                { 46409, new PetSpellData(150743, 150744, PetType.Attack) }, // Advanced Attendant-Droid
                { 46372, new PetSpellData(150743, 150744, PetType.Attack) }, // Faithful Attendant-Droid
                { 46367, new PetSpellData(150744, 150798, PetType.Attack) }, // Limited Attendant-Droid
                { 46403, new PetSpellData(150798, 150798, PetType.Attack) }, // Basic Attendant-Droid
                { 46392, new PetSpellData(150733, 150734, PetType.Attack) }, // Director-Grade Helper-Droid
                { 46382, new PetSpellData(150733, 150734, PetType.Attack) }, // Executive-Grade Helper-Droid
                { 46357, new PetSpellData(150734, 150735, PetType.Attack) }, // Supervisor-Grade Helper-Droid
                { 46411, new PetSpellData(150734, 150734, PetType.Attack) }, // Advanced Helper
                { 46374, new PetSpellData(150735, 150735, PetType.Attack) }, // Faithful Helper-Droid
                { 46359, new PetSpellData(150735, 150736, PetType.Attack) }, // Limited Helper-Bot
                { 46405, new PetSpellData(150735, 150736, PetType.Attack) }, // Basic Helper-Droid
                { 46386, new PetSpellData(150720, 150721, PetType.Attack) }, // Director-Grade Worker-Droid
                { 46385, new PetSpellData(150720, 150721, PetType.Attack) }, // Executive-Grade Worker-Droid
                { 46351, new PetSpellData(150721, 150721, PetType.Attack) }, // Supervisor-Grade Worker-Droid
                { 46399, new PetSpellData(150722, 150721, PetType.Attack) }, // Advanced Worker
                { 46363, new PetSpellData(150721, 150722, PetType.Attack) }, // Faithful Worker-Droid
                { 46362, new PetSpellData(150722, 96235, PetType.Attack) }, // Limited Worker-Droid
                { 46397, new PetSpellData(150722, 96235, PetType.Attack) } // Basic Worker-Droid
        };
    }
}
