using AOSharp.Common.GameData;
using System.Collections.Generic;
using static CombatHandler.Generic.GenericCombatHandler;

namespace CombatHandler.Engineer
{
    public class PetsList
    {
        public static readonly Dictionary<int, PetSpellData> Pets = new Dictionary<int, PetSpellData>
        {
            // Bots                
            { 223323, new PetSpellData(217994, 217994, PetType.Attack) },   //  Widowmaker Battle Drone
            { 223321, new PetSpellData(217993, 217993, PetType.Attack) },   //  Desolator Assault Drone
            { 223319, new PetSpellData(217992, 217992, PetType.Attack) },   //  Fieldsweeper Devastator Drone
            { 223317, new PetSpellData(217991, 217991, PetType.Attack) },   //  Battlefield Devastator Drone
            { 223315, new PetSpellData(217990, 217990, PetType.Attack) },   //  Devastator Drone

            { 223313, new PetSpellData(217989, 217989, PetType.Attack) },   //  Slayerdroid Annihilator
            { 45671, new PetSpellData(96218, 96218, PetType.Attack) },      //  Slayerdroid Guardian
            { 45673, new PetSpellData(150774, 96218, PetType.Attack) },     //  Slayerdroid Sentinel
            { 45674, new PetSpellData(150775, 96220, PetType.Attack) },     //  Slayerdroid Warden
            { 45672, new PetSpellData(150776, 150775, PetType.Attack) },    //  Slayerdroid Protector

            { 45685, new PetSpellData(150766, 150766, PetType.Attack) },    //  Semi-Sentient Wardroid
            { 45679, new PetSpellData(150767, 150766, PetType.Attack) },    //  Reactivated Wardroid
            { 45729, new PetSpellData(150768, 150767, PetType.Attack) },    //  Decommissioned Wardroid

            { 45690, new PetSpellData(150762, 150763, PetType.Attack) },    //  Military-Grade Warmachine
            { 45670, new PetSpellData(150762, 150763, PetType.Attack) },    //  Semi-Sentient Warmachine
            { 45678, new PetSpellData(150762, 150762, PetType.Attack) },    //  Perfected Warmachine
            { 45735, new PetSpellData(150761, 150762, PetType.Attack) },    //  Advanced Warmachine
            { 45667, new PetSpellData(150761, 150762, PetType.Attack) },    //  Upgraded Warmachine
            { 45669, new PetSpellData(150761, 150762, PetType.Attack) },    //  Warmachine
            { 45728, new PetSpellData(150764, 150761, PetType.Attack) },    //  Common Warmachine
            { 45722, new PetSpellData(150764, 150761, PetType.Attack) },    //  Flawed Warmachine
            { 45708, new PetSpellData(150764, 150761, PetType.Attack) },    //  Inferior Warmachine
            { 45688, new PetSpellData(150765, 150764, PetType.Attack) },    //  Lesser Warmachine
            { 45696, new PetSpellData(150765, 150764, PetType.Attack) },    //  Patchwork Warmachine
            { 45716, new PetSpellData(150765, 150764, PetType.Attack) },    //  Feeble Warmachine

            { 45689, new PetSpellData(150770, 150769, PetType.Attack) },    //  Military-Grade Warbot
            { 45684, new PetSpellData(150770, 150769, PetType.Attack) },    //  Semi-Sentient Warbot
            { 45677, new PetSpellData(150771, 150770, PetType.Attack) },    //  Perfected Warbot
            { 45734, new PetSpellData(150771, 150770, PetType.Attack) },    //  Advanced Warbot
            { 45666, new PetSpellData(150771, 150770, PetType.Attack) },    //  Upgraded Warbot
            { 45668, new PetSpellData(150771, 150771, PetType.Attack) },    //  Warbot
            { 45727, new PetSpellData(150773, 150771, PetType.Attack) },    //  Common Warbot
            { 45721, new PetSpellData(150773, 150771, PetType.Attack) },    //  Flawed Warbot
            { 45707, new PetSpellData(150773, 150771, PetType.Attack) },    //  Inferior Warbot
            { 45687, new PetSpellData(150772, 150773, PetType.Attack) },    //  Lesser Warbot
            { 45695, new PetSpellData(150772, 150773, PetType.Attack) },    //  Patchwork Warbot
            { 45715, new PetSpellData(150772, 150773, PetType.Attack) },    //  Feeble Warbot

            { 45683, new PetSpellData(150778, 150777, PetType.Attack) },    //  Semi-Sentient Guardbot
            { 45700, new PetSpellData(150778, 150777, PetType.Attack) },    //  Perfected Guardbot
            { 45733, new PetSpellData(150778, 150777, PetType.Attack) },    //  Advanced Guardbot
            { 45665, new PetSpellData(150778, 150778, PetType.Attack) },    //  Upgraded Guardbot
            { 45702, new PetSpellData(150779, 150778, PetType.Attack) },    //  Guardbot
            { 45726, new PetSpellData(150779, 150778, PetType.Attack) },    //  Common Guardbot
            { 45720, new PetSpellData(150779, 150778, PetType.Attack) },    //  Flawed Guardbot
            { 45706, new PetSpellData(150780, 150779, PetType.Attack) },    //  Inferior Guardbot
            { 45686, new PetSpellData(150780, 150779, PetType.Attack) },    //  Lesser Guardbot
            { 45694, new PetSpellData(150780, 150779, PetType.Attack) },    //  Patchwork Guardbot
            { 45714, new PetSpellData(150781, 150780, PetType.Attack) },    //  Feeble Guardbot

            { 45682, new PetSpellData(150782, 96215, PetType.Attack) },     //  Semi-Sentient Gladiatorbot
            { 45699, new PetSpellData(150783, 150782, PetType.Attack) },    //  Perfected Gladiatorbot
            { 45732, new PetSpellData(150783, 150782, PetType.Attack) },    //  Advanced Gladiatorbot
            { 45664, new PetSpellData(150783, 150782, PetType.Attack) },    //  Upgraded Gladiatorbot
            { 45701, new PetSpellData(150784, 150783, PetType.Attack) },    //  Gladiatorbot
            { 45725, new PetSpellData(150784, 150783, PetType.Attack) },    //  Common Gladiatorbot
            { 45719, new PetSpellData(150784, 150783, PetType.Attack) },    //  Flawed Gladiatorbot
            { 45705, new PetSpellData(150784, 150784, PetType.Attack) },    //  Inferior Gladiatorbot
            { 45711, new PetSpellData(150785, 150784, PetType.Attack) },    //  Lesser Gladiatorbot
            { 45693, new PetSpellData(150785, 150784, PetType.Attack) },    //  Patchwork Gladiatorbot
            { 45713, new PetSpellData(150785, 150784, PetType.Attack) },    //  Feeble Gladiatorbot

            { 45680, new PetSpellData(150791, 96228, PetType.Attack) },     //  Semi-Sentient Android
            { 45697, new PetSpellData(150791, 150791, PetType.Attack) },    //  Perfected Android
            { 45730, new PetSpellData(150792, 150791, PetType.Attack) },    //  Advanced Android
            { 45675, new PetSpellData(150793, 150792, PetType.Attack) },    //  Upgraded Android
            { 45736, new PetSpellData(150793, 150792, PetType.Attack) },    //  Android
            { 45723, new PetSpellData(150795, 150794, PetType.Attack) },    //  Common Android
            { 45717, new PetSpellData(150795, 150795, PetType.Attack) },    //  Flawed Android
            { 45703, new PetSpellData(150796, 150795, PetType.Attack) },    //  Inferior Android
            { 45709, new PetSpellData(150796, 150795, PetType.Attack) },    //  Lesser Android
            { 45691, new PetSpellData(150797, 150796, PetType.Attack) },    //  Patchwork Android
            { 45712, new PetSpellData(150797, 150796, PetType.Attack) },    //  Feeble Android

            { 45681, new PetSpellData(150786, 150786, PetType.Attack) },    //  Semi-Sentient Automaton
            { 45698, new PetSpellData(150787, 150786, PetType.Attack) },    //  Perfected Automaton
            { 45731, new PetSpellData(150787, 150786, PetType.Attack) },    //  Advanced Automaton
            { 45676, new PetSpellData(150788, 150787, PetType.Attack) },    //  Upgraded Automaton
            { 45737, new PetSpellData(150788, 150787, PetType.Attack) },    //  Automaton
            { 45718, new PetSpellData(150790, 150790, PetType.Attack) },    //  Flawed Automaton
            { 45724, new PetSpellData(150790, 150789, PetType.Attack) },    //  Common Automaton
            { 45704, new PetSpellData(96196, 150790, PetType.Attack) },     //  Inferior Automaton
            { 45710, new PetSpellData(96196, 150790, PetType.Attack) },     //  Lesser Automaton
            { 45692, new PetSpellData(96196, 150790, PetType.Attack) },     //  Patchwork Automaton
            { 43325, new PetSpellData(96196, 96196, PetType.Attack) },      //  Feeble Automaton

            //Dogs
            { 275815, new PetSpellData(275816, 275816, PetType.Support) },  //  Ravening M-60

            { 223337, new PetSpellData(218001, 218001, PetType.Support) },  //  Military-Grade Marauder M-45
            { 223335, new PetSpellData(218000, 218000, PetType.Support) },  //  Marauder M-45

            { 223333, new PetSpellData(217999, 217999, PetType.Support) },  //  Military-Grade Predator M-30
            { 223331, new PetSpellData(217998, 217998, PetType.Support) },  //  Semi-Sentient Predator M-30
            { 223329, new PetSpellData(217997, 217997, PetType.Support) },  //  Advanced Predator M-30
            { 223327, new PetSpellData(217996, 217996, PetType.Support) },  //  Upgraded Predator M-30
            { 301855, new PetSpellData(301857, 301857, PetType.Support) },  //  Predator M-30
            { 223325, new PetSpellData(217995, 217995, PetType.Support) }   //  Prototype Predator M-30
        };
    }
}
