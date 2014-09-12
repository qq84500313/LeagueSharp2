﻿#region
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Font = SharpDX.Direct3D9.Font;
using Rectangle = SharpDX.Rectangle;
#endregion

namespace Leblanc
{
    internal class Program
    {
        public const string ChampionName = "Leblanc";
        public static readonly Obj_AI_Hero vPlayer = ObjectManager.Player;

        private static readonly List<Texture> Enemies2 = new List<Texture>();
        private static readonly Dictionary<Obj_AI_Hero, Texture> Enemies = new Dictionary<Obj_AI_Hero, Texture>();
        private static Sprite SlideSprite;
        private static Font RecF;
        private static Texture wButton;
        private static Texture rButton;
        private static Texture picClockR;
        private static Texture picClockW;

        private static readonly List<Slide> ExistingSlide = new List<Slide>();
        private static bool leBlancClone;
        private static double soulShackleTimeExperies;

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q, W, E, R, SpellJump;

        public static SpellSlot IgniteSlot;

        public static Items.Item Fqc = new Items.Item(3092, 750); // Frost Queen's Claim; 
        public static Items.Item Dfg = Utility.Map.GetMap() != Utility.Map.MapType.SummonersRift ? new Items.Item(3188, 750) : new Items.Item(3128, 750);
        

        //Menu
        public static Menu Config;

        public static bool LeBlancClone
        {
            get
            {
                return leBlancClone;
            }
            set
            {
                leBlancClone = value;
            }
        }

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += new Program().Game_OnGameLoad;
        }

        private void Game_OnGameLoad(EventArgs args)
        {
            //if (vPlayer.BaseSkinName != ChampionName) return;
            Console.Clear();
            //Create the spells
            Q = new Spell(SpellSlot.Q, 720);
            W = new Spell(SpellSlot.W, 580);
            E = new Spell(SpellSlot.E, 950);
            R = new Spell(SpellSlot.R, 950);

            Q.SetTargetted(0.5f, float.MaxValue);
            W.SetSkillshot(0.5f, 200f, 1200f, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.5f, 100f, 1000f, true, SkillshotType.SkillshotLine);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            IgniteSlot = vPlayer.GetSpellSlot("SummonerDot");

            //Create the menu
            Config = new Menu(ChampionName, ChampionName, true);
            //Orbwalker submenu
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            //Add the target selector to the menu as submenu.
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            //Load the orbwalker and add it to the menu as submenu.
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            //Combo menu:
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseIgniteCombo", "Use Ignite").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseDFGCombo", "Use Deathfire Grasp").SetValue(true));
            Config.SubMenu("Combo")
                .AddItem(new MenuItem("ComboMode", "Combo Mode: ").SetValue(new StringList(new[] { "Q+R+W+E", "W+R+Q+E" })));
            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("ComboActive", "Combo!").SetValue(
                        new KeyBind(Config.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));

            //Harass menu:
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("Harass")
                .AddItem(new MenuItem("HarassMode", "Harass Mode: ").SetValue(new StringList(new[] { "Q+W+E", "W+Q+E" })));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassActive", "Harass!").SetValue(
                        new KeyBind(Config.Item("Farm").GetValue<KeyBind>().Key, KeyBindType.Press)));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassActiveT", "Harass (toggle)!").SetValue(new KeyBind("T".ToCharArray()[0],
                        KeyBindType.Toggle)));

            //Farming menu:
            Config.AddSubMenu(new Menu("Lane Clear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseWLaneClear", "Use W").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));

            Config.SubMenu("LaneClear")
                .AddItem(
                    new MenuItem("LaneClearActive", "Lane Clear!").SetValue(
                        new KeyBind(Config.Item("LaneClear").GetValue<KeyBind>().Key, KeyBindType.Press)));

            //JungleFarm menu:
            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJFarm", "Use W").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("JungleFarm")
                .AddItem(
                    new MenuItem("JungleFarmActive", "JungleFarm!").SetValue(
                        new KeyBind(Config.Item("LaneClear").GetValue<KeyBind>().Key, KeyBindType.Press)));

            //Misc
            Config.AddSubMenu(new Menu("Extras", "Extras"));
            Config.SubMenu("Extras").AddItem(new MenuItem("InterruptSpells", "Interrupt spells").SetValue(true));

            //Drawings menu:
            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("QRange", "Q Range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(100, 255, 0, 255))));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("WRange", "W Range").SetValue(new Circle(true, System.Drawing.Color.FromArgb(100, 255, 0, 255))));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("WObjectPosition", "W Object Position").SetValue(new Circle(true, System.Drawing.Color.FromArgb(100, 255, 0, 255))));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("WObjectTimeTick", "Show W Tick").SetValue(true));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("ERange", "E Range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(100, 255, 0, 255))));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("WQRange", "W+Q Range").SetValue(new Circle(false, System.Drawing.Color.GreenYellow)));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("EActiveRange", "E Active Range").SetValue(new Circle(false, System.Drawing.Color.GreenYellow)));
            Config.SubMenu("Drawings")

                .AddItem(new MenuItem("RRange", "R Range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(100, 255, 0, 255))));
            Config.AddToMainMenu();

            //Add the events we are going to use:
            Game.OnGameUpdate += Game_OnGameUpdate;

            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;

            Interrupter.OnPosibleToInterrupt += Interrupter_OnPosibleToInterrupt;

            Drawing.OnDraw += Drawing_OnDraw;
            
            Drawing.OnPreReset += Drawing_OnPreReset;
            Drawing.OnPostReset += Drawing_OnPostReset;
            Drawing.OnEndScene += Drawing_OnEndScene;
            
            Init();
            Game.PrintChat(String.Format("<font color='#70DBDB'>xQx </font> <font color='#FFFFFF'>{0}</font> <font color='#70DBDB'> Loaded!</font>", ChampionName));
        }

        /* ~Program()
         {
             Drawing.OnPreReset -= Drawing_OnPreReset;
             Drawing.OnPostReset -= Drawing_OnPostReset;
             Drawing.OnEndScene -= Drawing_OnEndScene;
         }
         */
        private static Obj_AI_Hero EnemyHaveSoulShackle
        {
            get
            {
                foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => vPlayer.Distance(hero) <= 1100))
                {
                    if (hero.IsEnemy)
                    {
                        foreach (BuffInstance buff in hero.Buffs)
                        {
                            if (buff.Name.Contains("LeblancSoulShackle"))
                            {
                                soulShackleTimeExperies = Game.Time + 2;
                                return hero;
                            }
                        }
                    }
                }
                soulShackleTimeExperies = 0;
                return null;
            }
        }
        private static bool DrawEnemySoulShackle
        {
            get
            {
                foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => vPlayer.Distance(hero) <= 1100))
                {
                    if (hero.IsEnemy)
                    {
                        foreach (BuffInstance buff in hero.Buffs)
                            return (buff.Name.Contains("LeblancSoulShackle"));
                    }
                }
                return false;
            }
        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!Config.Item("InterruptSpells").GetValue<bool>())
                return;
            if (E.IsReady())
            {
                E.Cast(unit);
            }
            else if (R.IsReady() && vPlayer.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancSlideM")
            {
                R.Cast(unit);
            }
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            leBlancClone = sender.Name.Contains("LeBlanc_MirrorImagePoff.troy");

            if (sender.Name.Contains("displacement_blink_indicator"))
            {
                ExistingSlide.Add(
                    new Slide
                    {
                        Object = sender,
                        NetworkId = sender.NetworkId,
                        Position = sender.Position,
                        ExpireTime = Game.Time + 3,
                        ExpireTimePicture = sender.Name.Contains("ult") ? picClockR : picClockW,
                        Picture = sender.Name.Contains("ult") ? rButton : wButton
                    });
            }
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            leBlancClone = sender.Name.Contains("LeBlanc_MirrorImagePoff.troy");

            if (sender.Name.Contains("displacement_blink_indicator"))
            {
                for (var i = 0; i < ExistingSlide.Count; i++)
                {
                    if (ExistingSlide[i].NetworkId == sender.NetworkId)
                    {
                        ExistingSlide.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        public static bool LeBlancStillJumped
        {
            //"LeblancSlide" // W is ready + LB can jump with W
            //"lblancslidereturn" LB jumped with W
            //"LeblancSlideM" // R-W is ready + LB can jump with R
            //"lblancslidereturnm" LB jumped with R
            get
            {
                if (W.IsReady() && vPlayer.Spellbook.GetSpell(SpellSlot.W).Name != "leblancslidereturn") // LB jumped with W
                {

                    SpellJump = W;
                    return false;
                }
                else if (R.IsReady() && vPlayer.Spellbook.GetSpell(SpellSlot.R).Name != "leblancslidereturnm") // LB jumped with R
                {

                    SpellJump = R;
                    return false;
                }

                return true;
            }

        }

        private static void UseSpellR(Obj_AI_Hero vTarget)
        {
            var rMode = vPlayer.Spellbook.GetSpell(SpellSlot.R).Name;
            switch (rMode)
            {
                case "LeblancChaosOrbM":
                    {
                        R.Range = Q.Range;
                        R.SetTargetted(0.5f, float.MaxValue);
                        R.CastOnUnit(vTarget);
                        break;
                    }
                case "LeblancSlideM":
                    {
                        R.Range = W.Range;
                        R.SetSkillshot(0.5f, 200f, float.MaxValue, false, SkillshotType.SkillshotCircle);
                        R.Cast(vTarget);
                        break;
                    }
                case "LeblancSoulShackleM":
                    {
                        R.Range = E.Range;
                        R.SetSkillshot(0.5f, 100f, 1000f, true, SkillshotType.SkillshotLine);
                        R.Cast(vTarget);
                        break;
                    }
            }

        }
        private static void Combo()
        {
            //            Game.PrintChat("W Name: " + vPlayer.Spellbook.GetSpell(SpellSlot.W).Name);
            //            if (R.Level > 0)
            //                Game.PrintChat("R Name: " + vPlayer.Spellbook.GetSpell(SpellSlot.R).Name);

            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();
            var useIgnite = Config.Item("UseIgniteCombo").GetValue<bool>();

            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            var wqTarget = SimpleTs.GetTarget(W.Range + Q.Range - 30, SimpleTs.DamageType.Magical);


           // Console.WriteLine("Q Damage: " + Q.GetDamage(wqTarget, DamageLib.StageType.Default).ToString());// DamageLib.getDmg(wqTarget, DamageLib.SpellType.Q).ToString());
            //Console.WriteLine("W Damage: " + DamageLib.getDmg(wqTarget, DamageLib.SpellType.W).ToString());
            //Console.WriteLine("E Damage: " + DamageLib.getDmg(wqTarget, DamageLib.SpellType.E).ToString());
           // Console.WriteLine("---------------------------------------------------------");



            if (useQ && qTarget != null && Q.IsReady())
            {
                Q.Cast(qTarget);
            }

            if (qTarget != null && Dfg.IsReady())
            {
                Dfg.Cast(qTarget);
            }

            if (qTarget != null && useIgnite && IgniteSlot != SpellSlot.Unknown &&
                vPlayer.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                if (vPlayer.Distance(qTarget) < 650 && GetComboDamage(qTarget) >= qTarget.Health)
                {
                    vPlayer.SummonerSpellbook.CastSpell(IgniteSlot, qTarget);
                }
            }

            if (useR && R.IsReady())
            {
                var rMode = vPlayer.Spellbook.GetSpell(SpellSlot.R).Name;
                switch (rMode)
                {
                    case "LeblancChaosOrbM":
                        {
                            R.Range = Q.Range;
                            R.SetTargetted(0.5f, float.MaxValue);
                            if (qTarget != null)
                                R.CastOnUnit(qTarget);
                            break;
                        }
                    case "LeblancSlideM":
                        {
                            R.Range = W.Range;
                            R.SetSkillshot(0.5f, 200f, float.MaxValue, false, SkillshotType.SkillshotCircle);
                            if (wTarget != null)
                                R.Cast(wTarget);
                            break;
                        }
                    case "LeblancSoulShackleM":
                        {
                            R.Range = E.Range;
                            R.SetSkillshot(0.5f, 100f, 1000f, true, SkillshotType.SkillshotLine);
                            if (eTarget != null)
                                R.Cast(eTarget);
                            break;
                        }
                }
            }

            if (useW && wTarget != null && W.IsReady() && !LeBlancStillJumped)
            {
                W.Cast(wTarget);
            }


            if (useE && eTarget != null && E.IsReady())
            {
                if (E.Cast(eTarget) == Spell.CastStates.SuccessfullyCasted)
                    return;
                //E.Cast(eTarget);
            }

        }

        private static void Harass()
        {
            var existsMana = vPlayer.MaxMana / 100 * Config.Item("JungleFarmMana").GetValue<Slider>().Value;
            if (vPlayer.Mana <= existsMana) return;

            UseSpells(
                Config.Item("UseQHarass").GetValue<bool>(),
                Config.Item("UseWHarass").GetValue<bool>(),
                Config.Item("UseEHarass").GetValue<bool>(),
                false,
                false
                );
        }

        private static float GetComboDamage(Obj_AI_Base vTarget)
        {
            var fComboDamage = 0d;

            if (Q.IsReady())
                fComboDamage += DamageLib.getDmg(vTarget, DamageLib.SpellType.Q);

            if (W.IsReady())
                fComboDamage += DamageLib.getDmg(vTarget, DamageLib.SpellType.W);

            if (E.IsReady())
                fComboDamage += DamageLib.getDmg(vTarget, DamageLib.SpellType.E, DamageLib.StageType.FirstDamage);

            if (IgniteSlot != SpellSlot.Unknown && vPlayer.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                fComboDamage += DamageLib.getDmg(vTarget, DamageLib.SpellType.IGNITE);

            if (Items.CanUseItem(3128))
                fComboDamage += DamageLib.getDmg(vTarget, DamageLib.SpellType.DFG);

            if (Items.CanUseItem(3092))
                fComboDamage += DamageLib.getDmg(vTarget, DamageLib.SpellType.FROSTQUEENSCLAIM);

            //   if (R.IsReady())
            //       fComboDamage += DamageLib.getDmg(vTarget, DamageLib.SpellType.R);

            return (float)fComboDamage;
        }

        private static void UseSpells(bool useQ, bool useW, bool useE, bool useR, bool useIgnite)
        {
            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);

            if (useE && eTarget != null && E.IsReady())
                E.Cast(eTarget);

            if (useW && wTarget != null && W.IsReady())
            {
                W.Cast(wTarget);
            }

            if (useQ && qTarget != null && Q.IsReady())
                Q.Cast(qTarget);


            if (qTarget != null && useIgnite && IgniteSlot != SpellSlot.Unknown &&
                vPlayer.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                if (vPlayer.Distance(qTarget) < 650 && GetComboDamage(qTarget) > qTarget.Health)
                {
                    vPlayer.SummonerSpellbook.CastSpell(IgniteSlot, qTarget);
                    UseItems(qTarget);
                }
            }

            if (useR && R.IsReady())
            {
                var rMode = vPlayer.Spellbook.GetSpell(SpellSlot.R).Name;
                switch (rMode)
                {
                    case "LeblancChaosOrbM":
                        {
                            //Game.PrintChat("R = Q");
                            R.Range = Q.Range;
                            R.SetTargetted(0.5f, float.MaxValue);
                            if (qTarget != null)
                                R.Cast(qTarget);
                            break;
                        }
                    case "LeblancSlideM":
                        {
                            //Game.PrintChat("R = W");
                            R.Range = W.Range;
                            R.SetSkillshot(0.5f, 200f, float.MaxValue, false, SkillshotType.SkillshotCircle);
                            if (wTarget != null)
                                R.CastIfWillHit(wTarget);
                            break;
                        }
                    case "LeblancSoulShackleM":
                        {
                            //Game.PrintChat("R = E");
                            R.Range = E.Range;
                            R.SetSkillshot(0.5f, 100f, 1000f, true, SkillshotType.SkillshotLine);
                            if (eTarget != null)
                                R.CastIfWillHit(eTarget);
                            break;
                        }
                }
            }
        }

        private static void LaneClear()
        {
            if (!Orbwalking.CanMove(40)) return;

            var existsMana = vPlayer.MaxMana / 100 * Config.Item("LaneClearMana").GetValue<Slider>().Value;
            if (vPlayer.Mana <= existsMana) return;

            var useQ = Config.Item("UseQLaneClear").GetValue<bool>();
            var useW = Config.Item("UseWLaneClear").GetValue<bool>();

            if (useQ && Q.IsReady())
            {
                var minionsQ = MinionManager.GetMinions(vPlayer.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.Health);
                foreach (Obj_AI_Base vMinion in minionsQ)
                {
                    var vMinionEDamage = DamageLib.getDmg(vMinion, DamageLib.SpellType.Q, DamageLib.StageType.FirstDamage);

                    if (vMinion.Health <= vMinionEDamage)
                    {
                        Q.CastOnUnit(vMinion);
                    }
                }
            }

            var rangedMinionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range + W.Width + 30);
            if (useW && W.IsReady())
            {
                var minionsW = W.GetCircularFarmLocation(rangedMinionsW, W.Width * 0.75f);
                if (minionsW.MinionsHit >= 3 && W.InRange(minionsW.Position.To3D()))
                {
                    W.Cast(minionsW.Position);
                    return;
                }
            }
        }

        private static void JungleFarm()
        {
            var existsMana = vPlayer.MaxMana / 100 * Config.Item("JungleFarmMana").GetValue<Slider>().Value;
            if (vPlayer.Mana <= existsMana) return;

            var useQ = Config.Item("UseQJFarm").GetValue<bool>();
            var useW = Config.Item("UseWJFarm").GetValue<bool>();
            var useE = Config.Item("UseEJFarm").GetValue<bool>();

            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (useQ && Q.IsReady())
                    Q.CastOnUnit(mob);

                if (useW && W.IsReady())
                    W.Cast(mob);

                if (useE && E.IsReady())
                    E.CastOnUnit(mob);
            }
        }


        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (vPlayer.IsDead) return;


            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.IsEnemy)
                {
                    foreach (BuffInstance buff in hero.Buffs)
                    {
                        if (buff.Name.Contains("blanc"))
                        {
                            //Game.PrintChat(hero.BaseSkinName + " have : " + buff.Name);
                        }
                    }
                }
            }

            Orbwalker.SetAttacks(true);
            /*
                        foreach (var buff in vPlayer.Buffs)
                        {
                            if (buff.Name.Contains("lone"))
                            {
                                Game.PrintChat(buff.Name);
                            }
                        }
            */

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                if (Config.Item("HarassActive").GetValue<KeyBind>().Active ||
                    Config.Item("HarassActiveT").GetValue<KeyBind>().Active)
                {
                    Harass();
                }

                if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
                    LaneClear();

                if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
                    JungleFarm();
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active && spell.Level > 0)
                    Utility.DrawCircle(vPlayer.Position, spell.Range, menuItem.Color, 1, 10);
            }
            /*
            var size = 10;
            var topLeft = new Vector2(vPlayer.Position.X - 10 * size, vPlayer.Position.Y - 10 * size);
            var topRight = new Vector2(vPlayer.Position.X + 10 * size, vPlayer.Position.Y - 10 * size);
            var botLeft = new Vector2(vPlayer.Position.X - 10 * size, vPlayer.Position.Y + 10 * size);
            var botRight = new Vector2(vPlayer.Position.X + 10 * size, vPlayer.Position.Y + 10 * size);

            Drawing.DrawLine(topLeft.X, topLeft.Y, botRight.X, botRight.Y, 10, Color.Red);
            Drawing.DrawLine(topRight.X, topRight.Y, botLeft.X, botLeft.Y, 10, Color.Red);
            */

            var wObjectPosition = Config.Item("WObjectPosition").GetValue<Circle>();
            var wObjectTimeTick = Config.Item("WObjectTimeTick").GetValue<bool>();

            var eActiveRange = Config.Item("EActiveRange").GetValue<Circle>();


            if (eActiveRange.Active)
            {
                var vTarget = EnemyHaveSoulShackle;
                if (vTarget != null)
                {
                  /*  TimeSpan time = TimeSpan.FromSeconds(soulShackleTimeExperies - Game.Time);
                    Vector2 pos = Drawing.WorldToScreen(vTarget.Position);
                    string display = string.Format("{0}:{1:D2}", time.Minutes, time.Seconds);
                    Drawing.DrawText(pos.X - display.Length * 3, pos.Y + 20, System.Drawing.Color.YellowGreen, display);
                    */
                    Utility.DrawCircle(vPlayer.Position, 1100f, eActiveRange.Color);
                }
            }

            /*  if (DrawEnemySoulShackle && eActiveRange.Active)
              {
                  Utility.DrawCircle(vPlayer.Position, 1100f, eActiveRange.Color);
              }
              */
            foreach (var existingSlide in ExistingSlide)
            {
                if (wObjectPosition.Active)
                    Utility.DrawCircle(existingSlide.Position, 110f, wObjectPosition.Color);

                if (wObjectTimeTick)
                {
                    if (existingSlide.ExpireTime > Game.Time)
                    {
                        Vector2 pos;
                        string display;
                        
                        /*
                        float existingSlidePosition = 66;
                        if (existingSlide.ExpireTimePicture == picClockR)
                            existingSlidePosition = 122;
                        */
                        TimeSpan time = TimeSpan.FromSeconds(existingSlide.ExpireTime - Game.Time);

                        //pos = Drawing.WorldToScreen(vPlayer.Position);
                        //display = string.Format("{0}:{1:D2}", time.Minutes, time.Seconds);
                        //Drawing.DrawText(pos.X - display.Length * 3, pos.Y - 65 - existingSlidePosition, System.Drawing.Color.GreenYellow, display);

                        pos = Drawing.WorldToScreen(existingSlide.Position);
                        display = string.Format("{0}:{1:D2}", time.Minutes, time.Seconds);
                        Drawing.DrawText(pos.X - display.Length * 3, pos.Y - 65, System.Drawing.Color.GreenYellow, display);
                    }
                }
            }
        }

        void Drawing_OnEndScene(EventArgs args)
        {
            try
            {
                foreach (var existingSlide in ExistingSlide)
                {
                    float percentScale = 4;
                    float percentclockScale = 2;
                    float picturePosition = 62;
                    SlideSprite.Begin();
                    foreach (Texture enemy in Enemies2)
                    {
                        Vector2 serverPos = Drawing.WorldToScreen(existingSlide.Position);
                        Vector2 playerServerPos = Drawing.WorldToScreen(vPlayer.Position);
                        Size mPos = new Size((int)(serverPos[0] - 62 * 0.3f), (int)(serverPos[1] - 62 * 0.3f));

                        DirectXDrawer.DrawSprite(SlideSprite, existingSlide.Picture,
                            mPos.ScaleSize(percentScale, new Vector2(mPos.Width, mPos.Height)),
                            new[] { 0.3f * percentScale, 0.3f * percentScale });

                        /*if (existingSlide.ExpireTimePicture == picClockR)
                            picturePosition = 122;
                        Size cPos = new Size((int)(playerServerPos[0] - 82 * 0.3f), (int)(playerServerPos[1] + picturePosition * 0.3f));
                        DirectXDrawer.DrawSprite(SlideSprite, existingSlide.ExpireTimePicture,
                            cPos.ScaleSize(percentclockScale, new Vector2(cPos.Width, cPos.Height)),
                            new[] { 0.3f * percentclockScale, 0.3f * percentclockScale });
                        */
                    }
                    SlideSprite.End();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                if (ex.GetType() == typeof(SharpDXException))
                {
                    Game.PrintChat("An error occured. Please re-load LeBlanc app.");
                }
            }
        }

        public static void Init()
        {
            try
            {
                SlideSprite = new Sprite(Drawing.Direct3DDevice);
                RecF = new Font(Drawing.Direct3DDevice, new System.Drawing.Font("Tahoma", 9));
            }
            catch (Exception)
            {
                return;
            }

            var picLocation = Assembly.GetExecutingAssembly().Location;
            picLocation = picLocation.Remove(picLocation.LastIndexOf("\\", StringComparison.Ordinal));
            picLocation = picLocation + "\\Sprites\\xQx\\LeBlanc\\";
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.IsMe)
                {
                    if (!File.Exists(picLocation + "W2.png"))
                    {
                        Game.PrintChat(picLocation + "W2.png file not found!");
                    }
                    else
                    {
                        SpriteHelper.LoadTexture(picLocation + "W2.png", ref wButton);
                        Enemies2.Add(wButton);
                    }

                    if (!File.Exists(picLocation + "R2.png"))
                    {
                        Game.PrintChat(picLocation + "R2.png file not found!");
                    }
                    else
                    {
                        SpriteHelper.LoadTexture(picLocation + "R2.png", ref rButton);
                        Enemies2.Add(rButton);
                    }
                    if (!File.Exists(picLocation + "clockW.png"))
                    {
                        Game.PrintChat(picLocation + "clockW.png file not found!");
                    }
                    else
                    {
                        SpriteHelper.LoadTexture(picLocation + "clockW.png", ref picClockW);
                        Enemies2.Add(picClockW);
                    }
                    if (!File.Exists(picLocation + "clockR.png"))
                    {
                        Game.PrintChat(picLocation + "clockR.png file not found!");
                    }
                    else
                    {
                        SpriteHelper.LoadTexture(picLocation + "clockR.png", ref picClockR);
                        Enemies2.Add(picClockR);
                    }

                }
            }
            Game.PrintChat("Init Done!");
        }

        void Drawing_OnPostReset(EventArgs args)
        {
            SlideSprite.OnResetDevice();
            RecF.OnResetDevice();
        }

        void Drawing_OnPreReset(EventArgs args)
        {
            SlideSprite.OnLostDevice();
            RecF.OnLostDevice();
        }

        public static void UseItems(Obj_AI_Hero vTarget)
        {
        }
    }
}
