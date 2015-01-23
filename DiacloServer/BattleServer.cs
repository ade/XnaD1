using System;
using System.Collections.Generic;
using System.Text;
using DiacloLib;

namespace DiacloServer
{

    public static class BattleServer
    {
        private static Random rnd = new Random();

        public static BattleResult MonsterVsPlayer(ServerNPC npc, Player player)
        {
            BattleResult result = new BattleResult(npc, player);
            result.Type = BattleTypes.MonsterVsPlayer;
            if (player.CurrentHP <= 0)
            {
                result.Outcome = BattleOutcome.Immune;
            }
            else if (HitRollMVP(npc, player)) //Make a roll if the npc will hit player or not
            {
                result.Outcome = BattleOutcome.Hit;
                int damage = DamageRollMVP(npc, player);
                
                result.TargetTakeDamage = damage;
                result.TargetNewHP = player.CurrentHP - damage;
            }
            else
            {
                result.Outcome = BattleOutcome.Miss;
            }
            return result;
        }

        private static int DamageRollMVP(ServerNPC npc, Player player)
        {
            /* Jarulf says...
            1. calculate Rnd[maxbase damage - minbase damage + 1] + minbase damage (this is basically a random value within the
            monsters base damage range)
            2. add -damage
            3. if damage is below 1, set it to 1
            4. in Hellfire, if the player has a Reflect spell running, calculate the final reflected damage (FRD) by multiplying
            the damage by (Rnd[10] + 20)/100
            5. in Hellfire, if the player has a Reflect spell running, subtract FRD from the damage calculated in steps 1-3
            6. the value now achieved is the final damage (FD)
            7. deal FD to target
             */
            int basedmg = rnd.Next(npc.DamageMin, npc.DamageMax);
            if (basedmg < 1) basedmg = 1;
            return basedmg;
        }
        private static bool HitRollMVP(ServerNPC npc, Player player) {

            int tohit = 30 + npc.ToHit + 2 * npc.Level;
            //calculate Dex/5 + Acitems-player + bonusplayer + 2·clvl (todo)
            int penalty = player.Character.AttDex / 5 + 2 * player.Character.Level;
            tohit -= penalty;

            /* Jarulf says...
            6. on dlvl 14 if the value calculated is below 20, set it to 20
            7. on dlvl 15 if the value calculated is below 25, set it to 25
            8. on dlvl 16 if the value calculated is below 30, set it to 30
            (todo)  */ 
            if (tohit < 15) tohit = 15;


            //Roll
            if (rnd.Next(0, 99) < tohit)
                return true;
            else
                return false;
            
        }
        private static bool HitRollPVM(Player p, ServerNPC npc)
        {
            /* Jarulf says...
                - calculate 50 + Dex/2 + ToHititems + clvl + bonusplayer
                - subtract Acmonster
                - if the value calculated is below 5, set it to 5
                - if the value calculated is above 95, set it to 95
                - the value now achieved is the final chance to hit (FTH)
                - a hit is secured if Rnd[100] < FTH
             */
            int bonus = 0;
            if (p.Character.Class == CharacterClass.Warrior) bonus = 20; //ugly-- todo: move to player's data
            int tohit = 50 + p.Character.AttDex / 2 + p.Character.Level + bonus;
            tohit -= npc.ArmorClass;
            if (tohit < 5) tohit = 5;
            if (tohit > 95) tohit = 95;
            if (rnd.Next(0, 99) < tohit)
                return true;
            else
                return false;

        }
        public static BattleResult PlayerMeleeVsMonster(Player p, ServerNPC npc)
        {
            BattleResult result = new BattleResult(p, npc);
            result.Type = BattleTypes.PlayerVsMonster;

            if (HitRollPVM(p, npc))
            {
                result.Outcome = BattleOutcome.Hit;
                int damage = DamageRollPVM(p, npc);
                result.TargetTakeDamage = damage;
                result.TargetNewHP = npc.CurrentHP - damage;
            }
            else
            {
                result.Outcome = BattleOutcome.Miss;
            }
            return result;
            

        }
        public static int DamageRollPVM(Player p, ServerNPC npc) {
            /* Jarulf says...
             *  - calculate Rnd[maxbase weapon damage - minbase weapon damage + 1] + minbase weapon damage (this is basically a random
                    value within the weapons base damage range)
                - add +% damage
                - add +damage
                - add character damage
                - if the attacker is a Warrior then double damage if Rnd[100] < clvl (critical hit)
                - if the monster is undead or an animal, adjust the damage according to the table in chapter 5.1 under
                monster type, that is, under certain circumstances multiply damage by 1.5 or divide damage by 2
                - if the monster is a demon, triple the damage if the player is carrying any item with the ”+200% damage
                versus demons” effect
                - the value now achieved is the final damage (FD): deal FD to the target
             */
            int dmg = rnd.Next(1, 4); //(small axe!)
            dmg += rnd.Next(p.DamageMin, p.DamageMax);

            if (p.Character.Class == CharacterClass.Warrior)
                if (rnd.Next(0, 99) < p.Character.Level)
                    dmg *= 2;

            return dmg;

        }
      
    }
}
