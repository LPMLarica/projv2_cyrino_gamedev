using System;
using System.Collections;
using System.Collections.Generic;
using Nexus.Core;
using UnityEngine;

namespace Nexus
{
    public class BattleDirector : MonoBehaviour
    {
        [Header("Cena")]
        public BoardLayout layout;
        public CameraRig cameraRig;
        public StructureView playerNexus;
        public StructureView enemyNexus;
        public StructureView enemyTurret;
        public GateController gate;
        public HUDController hud;

        [Header("Prefabs")]
        public CardView cardPrefab;
        public Projectile projectilePrefab;
        public DamagePopup popupPrefab;

        public Match Match { get; private set; }
        public int HoveredHandUid { get; set; }
        public int SelectedHandUid { get; set; }

        readonly Dictionary<int, CardView> views = new Dictionary<int, CardView>();
        bool busy = true;
        bool started;
        bool minionAnnounced;
        int playerCombos;
        MatchResult pendingResult = MatchResult.None;

        public bool AcceptingInput
        {
            get { return started && !busy && Match != null && !Match.IsOver && Match.Turn == Side.Player; }
        }

        public CardView ViewOf(CardInstance c)
        {
            CardView v;
            return c != null && views.TryGetValue(c.Uid, out v) ? v : null;
        }

        void Awake()
        {
            HoveredHandUid = -1;
            SelectedHandUid = -1;
        }

        void Start()
        {
            AudioManager.Music(MusicTrack.Battle);
            Match = new Match(Environment.TickCount);
            if (GameSettings.Hard) Match.TurretDamage = 2;
            hud.SetBest(GameSettings.BestScore);
            hud.SetScore(0, 0, "");
            hud.SetEndTurn(false, false);
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            if (cameraRig != null) yield return cameraRig.Intro();
            yield return Present(Match.StartMatch());
            playerNexus.SetHp(Match.Player.NexusHp, Match.Player.NexusMax);
            enemyNexus.SetHp(Match.Enemy.NexusHp, Match.Enemy.NexusMax);
            enemyTurret.SetHp(Match.TurretHp, Match.TurretMax);
            hud.SetNexus(Side.Player, Match.Player.NexusHp, Match.Player.NexusMax);
            hud.SetNexus(Side.Enemy, Match.Enemy.NexusHp, Match.Enemy.NexusMax);
            hud.SetTurret(Match.TurretHp, Match.TurretMax);
            hud.Banner("DESAFIO 1", "Cerco à Torre  ·  destrua a Torre para abrir o Portão", UIStyle.Gold, 2.6f);
            started = true;
            yield return BeginTurnFlow(Side.Player);
        }

        void LateUpdate()
        {
            if (Match == null) return;
            var hand = Match.Player.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                var v = ViewOf(hand[i]);
                if (v == null) continue;
                bool lifted = hand[i].Uid == HoveredHandUid || hand[i].Uid == SelectedHandUid;
                Vector3 p;
                Quaternion r;
                float s;
                layout.HandPose(i, hand.Count, lifted, out p, out r, out s);
                v.SetHandTarget(p, r, s);
            }
            hud.SetEndTurn(AcceptingInput, AcceptingInput && !HasUsefulAction());
        }

        bool HasUsefulAction()
        {
            string reason;
            foreach (var c in Match.Player.Hand)
                if (Match.CanPlay(Side.Player, c, out reason)) return true;
            foreach (var u in Match.Player.Units())
                if (u.CanAttack) return true;
            return false;
        }

        // ---------- Ações do jogador ----------

        public bool TryPlayCard(CardInstance card, int slot)
        {
            if (!AcceptingInput) return false;
            string reason;
            if (!Match.CanPlay(Side.Player, card, out reason))
            {
                hud.Toast(reason, UIStyle.Crimson);
                return false;
            }
            if (card.Def.NeedsTarget) return false;
            StartCoroutine(PlayerAction(Match.PlayCard(Side.Player, card, slot, default(TargetRef))));
            return true;
        }

        public bool TryCastSpell(CardInstance spell, TargetRef target)
        {
            if (!AcceptingInput) return false;
            if (!Match.IsValidSpellTarget(spell, target))
            {
                hud.Toast("Alvo inválido para " + spell.Def.Name + ".", UIStyle.Crimson);
                return false;
            }
            StartCoroutine(PlayerAction(Match.PlayCard(Side.Player, spell, -1, target)));
            return true;
        }

        public bool TryAttack(CardInstance attacker, TargetRef target)
        {
            if (!AcceptingInput) return false;
            string reason;
            bool blocked;
            if (!Match.ValidateAttack(attacker, target, out reason, out blocked))
            {
                hud.Toast(reason, UIStyle.Crimson);
                return false;
            }
            StartCoroutine(PlayerAttack(attacker, target));
            return true;
        }

        public void RequestEndTurn()
        {
            if (!AcceptingInput) return;
            AudioManager.Fire(Sfx.Click);
            StartCoroutine(EndPlayerTurn());
        }

        IEnumerator PlayerAction(List<GameEvent> events)
        {
            busy = true;
            yield return Present(events);
            busy = false;
        }

        IEnumerator PlayerAttack(CardInstance attacker, TargetRef target)
        {
            busy = true;
            yield return AttackFlow(attacker, target);
            busy = false;
        }

        // ---------- Fluxo de turnos ----------

        IEnumerator BeginTurnFlow(Side side)
        {
            busy = true;
            yield return Present(Match.BeginTurn(side));
            if (Match.IsOver) yield break;
            if (side == Side.Player)
            {
                hud.Toast("Sua vez", UIStyle.Teal);
                busy = false;
            }
            else
            {
                hud.Toast("Vez do inimigo", UIStyle.Crimson);
                yield return EnemyTurnFlow();
            }
        }

        IEnumerator EndPlayerTurn()
        {
            busy = true;
            yield return Present(Match.EndTurn(Side.Player));
            yield return BeginTurnFlow(Side.Enemy);
        }

        IEnumerator EnemyTurnFlow()
        {
            yield return new WaitForSeconds(0.6f);
            int guard = 0;
            while (!Match.IsOver && guard++ < 40)
            {
                var action = AIPlanner.Plan(Match, Side.Enemy);
                if (action.Kind == AIActionKind.EndTurn) break;
                if (action.Kind == AIActionKind.Play)
                {
                    var ev = Match.PlayCard(Side.Enemy, action.Card, action.Slot, action.Target);
                    if (ev.Count == 0) break;
                    yield return Present(ev);
                }
                else
                {
                    yield return AttackFlow(action.Card, action.Target);
                }
                yield return new WaitForSeconds(0.25f);
            }
            if (Match.IsOver) yield break;
            if (Match.TurretActive) yield return TurretFlow();
            if (Match.IsOver) yield break;
            yield return Present(Match.EndTurn(Side.Enemy));
            yield return BeginTurnFlow(Side.Player);
        }

        IEnumerator AttackFlow(CardInstance attacker, TargetRef target)
        {
            var av = ViewOf(attacker);
            string reason;
            bool willBeBlocked;
            if (av == null || !Match.ValidateAttack(attacker, target, out reason, out willBeBlocked)) yield break;

            av.SetHighlight(CardHighlight.Selected);
            av.PlayAttack(attacker.Owner == Side.Player);
            AudioManager.Fire(Sfx.Attack);
            yield return new WaitForSeconds(0.14f);

            ProjectileOutcome? outcome = null;
            var color = attacker.Owner == Side.Player ? UIStyle.Teal : UIStyle.Crimson;
            LaunchProjectile(av.aim.position, AimPoint(target), color, av.gameObject, o => outcome = o);
            float waited = 0f;
            while (outcome == null && waited < 3.6f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            bool blocked = outcome == ProjectileOutcome.Blocked || (outcome != ProjectileOutcome.Hit && willBeBlocked);
            if (blocked && cameraRig != null) cameraRig.Shake(0.06f, 0.25f);
            if (av != null) av.SetHighlight(CardHighlight.None);
            yield return Present(Match.ResolveAttack(attacker, target, blocked));
        }

        IEnumerator TurretFlow()
        {
            var target = Match.ChooseTurretTarget();
            ProjectileOutcome? outcome = null;
            AudioManager.Fire(Sfx.Turret);
            LaunchProjectile((enemyTurret.muzzle != null ? enemyTurret.muzzle : enemyTurret.aim).position, AimPoint(target), new Color(1f, 0.35f, 0.25f), enemyTurret.gameObject, o => outcome = o);
            float waited = 0f;
            while (outcome == null && waited < 3.6f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            yield return Present(Match.ResolveTurretShot(target));
        }

        void LaunchProjectile(Vector3 from, Vector3 to, Color color, GameObject ignore, Action<ProjectileOutcome> done)
        {
            var p = Instantiate(projectilePrefab, from, Quaternion.identity);
            p.Launch(from, to, color, ignore, done);
        }

        Vector3 AimPoint(TargetRef t)
        {
            switch (t.Kind)
            {
                case TargetKind.Champion:
                    var v = ViewOf(t.Card);
                    return v != null ? v.transform.position + Vector3.up * 0.04f : layout.SlotPosition(t.Side, Mathf.Max(0, t.Card.Slot));
                case TargetKind.Turret:
                    return enemyTurret.aim.position;
                default:
                    return (t.Side == Side.Player ? playerNexus : enemyNexus).aim.position;
            }
        }

        // ---------- Apresentação de eventos ----------

        IEnumerator Present(List<GameEvent> events)
        {
            for (int i = 0; i < events.Count; i++)
                yield return PresentOne(events[i]);
            SyncAll();
            if (pendingResult != MatchResult.None)
            {
                var r = pendingResult;
                pendingResult = MatchResult.None;
                StartCoroutine(GameOverFlow(r));
            }
        }

        IEnumerator PresentOne(GameEvent e)
        {
            var drawn = e as CardDrawnEvent;
            if (drawn != null) { yield return OnDrawn(drawn); yield break; }

            var played = e as CardPlayedEvent;
            if (played != null) { yield return OnPlayed(played.Card, played.Slot, false); yield break; }

            var minion = e as MinionSummonedEvent;
            if (minion != null)
            {
                yield return OnPlayed(minion.Card, minion.Slot, true);
                if (!minionAnnounced)
                {
                    minionAnnounced = true;
                    hud.Toast("O Nexus convocou uma Tropa Guardiã!", UIStyle.Crimson);
                }
                yield break;
            }

            var spell = e as SpellCastEvent;
            if (spell != null) { yield return OnSpell(spell); yield break; }

            var damaged = e as CardDamagedEvent;
            if (damaged != null)
            {
                var v = ViewOf(damaged.Card);
                if (v != null)
                {
                    v.PlayHit();
                    v.Refresh(Match.Turn == v.Card.Owner);
                    Popup("-" + damaged.Amount, UIStyle.Crimson, v.transform.position, 1.2f);
                }
                yield return new WaitForSeconds(0.08f);
                yield break;
            }

            var healed = e as CardHealedEvent;
            if (healed != null)
            {
                var v = ViewOf(healed.Card);
                if (v != null) Popup("+" + healed.Amount, UIStyle.Heal, v.transform.position, 1.1f);
                yield break;
            }

            var stunned = e as StunnedEvent;
            if (stunned != null)
            {
                var v = ViewOf(stunned.Card);
                if (v != null)
                {
                    v.Refresh(false);
                    Popup("ATORDOADO", UIStyle.Stun, v.transform.position, 0.8f);
                }
                yield break;
            }

            var died = e as CardDiedEvent;
            if (died != null)
            {
                var v = ViewOf(died.Card);
                views.Remove(died.Card.Uid);
                if (v != null) v.Die();
                AudioManager.Fire(Sfx.Death);
                yield return new WaitForSeconds(0.15f);
                yield break;
            }

            var structure = e as StructureChangedEvent;
            if (structure != null) { OnStructure(structure); yield return new WaitForSeconds(0.1f); yield break; }

            if (e is TurretDestroyedEvent)
            {
                enemyTurret.Collapse();
                if (cameraRig != null) cameraRig.Shake(0.22f, 0.7f);
                AudioManager.Fire(Sfx.Death);
                hud.SetTurret(0, Match.TurretMax);
                yield return new WaitForSeconds(0.7f);
                yield break;
            }

            if (e is GateOpenedEvent)
            {
                gate.Open();
                hud.Toast("O Portão se abriu!", UIStyle.Gold);
                yield return new WaitForSeconds(1.2f);
                yield break;
            }

            var phase = e as PhaseChangedEvent;
            if (phase != null)
            {
                hud.SetPhase(phase.Phase);
                if (started && phase.Phase == GamePhase.NexusExposed)
                    hud.Banner("DESAFIO 2", "Nexus exposto  ·  Tropas Guardiãs surgem a cada turno", UIStyle.Crimson, 2.6f);
                yield break;
            }

            var stats = e as StatsChangedEvent;
            if (stats != null) { RefreshSide(stats.Side); yield break; }

            var region = e as RegionSynergyEvent;
            if (region != null)
            {
                string who = region.Side == Side.Player ? "Sinergia" : "Inimigo ativou sinergia";
                hud.Toast(who + " de " + CardDatabase.RegionName(region.Region) + ": " + SynergyRules.RegionBonusText(region.Region),
                    region.Side == Side.Player ? UIStyle.RegionColor(region.Region) : UIStyle.Crimson);
                yield break;
            }

            var combo = e as ComboEvent;
            if (combo != null)
            {
                if (combo.Side == Side.Player) playerCombos++;
                var a = ViewOf(combo.A);
                var b = ViewOf(combo.B);
                if (a != null) a.FlashCombo();
                if (b != null) b.FlashCombo();
                AudioManager.Fire(Sfx.Combo);
                hud.Banner((combo.Side == Side.Player ? "COMBO  ·  " : "COMBO INIMIGO  ·  ") + combo.Combo.Name.ToUpperInvariant(), combo.Combo.Text,
                    combo.Side == Side.Player ? UIStyle.Gold : UIStyle.Crimson);
                yield return new WaitForSeconds(0.9f);
                yield break;
            }

            var score = e as ScoreEvent;
            if (score != null) { hud.SetScore(score.Total, score.Delta, score.Label); yield break; }

            if (e is AttackBlockedEvent)
            {
                hud.Toast("O Portão bloqueou o ataque! Destrua a Torre primeiro.", UIStyle.Crimson);
                yield return new WaitForSeconds(0.3f);
                yield break;
            }

            var mana = e as ManaChangedEvent;
            if (mana != null)
            {
                if (mana.Side == Side.Player) hud.SetMana(mana.Mana, mana.Max);
                yield break;
            }

            var turn = e as TurnStartedEvent;
            if (turn != null) { hud.SetTurn(turn.Side, turn.Turn); yield break; }

            var burned = e as CardBurnedEvent;
            if (burned != null)
            {
                if (burned.Side == Side.Player) hud.Toast("Mão cheia: " + burned.Def.Name + " foi descartada.", UIStyle.Muted);
                yield break;
            }

            var fatigue = e as FatigueEvent;
            if (fatigue != null)
            {
                hud.Toast((fatigue.Side == Side.Player ? "Seu baralho acabou: " : "Baralho inimigo vazio: ") + fatigue.Damage + " de dano no Nexus.", UIStyle.Crimson);
                yield break;
            }

            var over = e as GameOverEvent;
            if (over != null) pendingResult = over.Result;
        }

        IEnumerator OnDrawn(CardDrawnEvent e)
        {
            if (e.Side == Side.Enemy)
            {
                layout.enemyDeck.SetCount(Match.Enemy.Deck.Count);
                yield break;
            }
            var v = Spawn(e.Card);
            v.transform.position = layout.playerDeck.TopPosition;
            v.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            layout.playerDeck.SetCount(Match.Player.Deck.Count);
            int index = Match.Player.Hand.IndexOf(e.Card);
            Vector3 p;
            Quaternion r;
            float s;
            layout.HandPose(Mathf.Max(0, index), Match.Player.Hand.Count, false, out p, out r, out s);
            AudioManager.Fire(Sfx.Draw);
            yield return v.FlyTo(p, r, s, 0.34f, 1.2f);
            v.EnterHand();
        }

        IEnumerator OnPlayed(CardInstance card, int slot, bool fromNexus)
        {
            Vector3 slotPos = layout.SlotPosition(card.Owner, slot);
            var v = ViewOf(card);
            if (v == null)
            {
                v = Spawn(card);
                Quaternion showRot;
                Vector3 show = layout.ShowcasePoint(out showRot);
                v.transform.position = fromNexus ? enemyNexus.aim.position : layout.enemyDeck.TopPosition;
                v.transform.rotation = showRot;
                v.transform.localScale = Vector3.one * 0.3f;
                if (!fromNexus)
                {
                    yield return v.FlyTo(show, showRot, 1.15f, 0.32f);
                    yield return new WaitForSeconds(0.5f);
                }
            }
            yield return v.FlyTo(slotPos + Vector3.up * 1.5f, Quaternion.identity, 1f, 0.26f, 0.2f);
            yield return v.DropToSlot(slotPos, 1.5f);
            v.Refresh(Match.Turn == card.Owner);
        }

        IEnumerator OnSpell(SpellCastEvent e)
        {
            var v = ViewOf(e.Card);
            if (v == null)
            {
                v = Spawn(e.Card);
                v.transform.position = layout.enemyDeck.TopPosition;
            }
            Quaternion showRot;
            Vector3 show = layout.ShowcasePoint(out showRot);
            yield return v.FlyTo(show, showRot, 1.15f, 0.3f);
            AudioManager.Fire(Sfx.Combo, 0.6f);
            if (e.HasTarget && e.Target.Card != null)
            {
                var target = ViewOf(e.Target.Card);
                if (target != null)
                {
                    target.FlashCombo();
                    if (e.Card.Def.Spell == SpellEffect.Ignite)
                    {
                        ProjectileOutcome? outcome = null;
                        LaunchProjectile(show, target.transform.position + Vector3.up * 0.04f, new Color(1f, 0.55f, 0.15f), v.gameObject, o => outcome = o);
                        float waited = 0f;
                        while (outcome == null && waited < 2f)
                        {
                            waited += Time.deltaTime;
                            yield return null;
                        }
                    }
                }
            }
            yield return new WaitForSeconds(0.3f);
            views.Remove(e.Card.Uid);
            v.Die();
        }

        void OnStructure(StructureChangedEvent e)
        {
            StructureView sv = e.Kind == TargetKind.Turret ? enemyTurret : e.Side == Side.Player ? playerNexus : enemyNexus;
            sv.SetHp(e.Hp, e.Max);
            if (e.Delta < 0)
            {
                sv.PlayHit();
                Popup(e.Delta.ToString(), UIStyle.Crimson, sv.aim.position, 1.5f);
                if (cameraRig != null && e.Kind == TargetKind.Nexus) cameraRig.Shake(0.07f, 0.25f);
            }
            else Popup("+" + e.Delta, UIStyle.Heal, sv.aim.position, 1.3f);

            if (e.Kind == TargetKind.Turret) hud.SetTurret(e.Hp, e.Max);
            else hud.SetNexus(e.Side, e.Hp, e.Max);
        }

        CardView Spawn(CardInstance card)
        {
            var v = Instantiate(cardPrefab);
            v.name = "Carta_" + card.Def.Id + "_" + card.Uid;
            v.Bind(card);
            views[card.Uid] = v;
            return v;
        }

        void Popup(string text, Color color, Vector3 position, float scale)
        {
            if (popupPrefab == null) return;
            var p = Instantiate(popupPrefab);
            p.Show(text, color, position, scale);
        }

        void RefreshSide(Side side)
        {
            foreach (var u in Match.Get(side).Units())
            {
                var v = ViewOf(u);
                if (v != null) v.Refresh(Match.Turn == side);
            }
        }

        void SyncAll()
        {
            RefreshSide(Side.Player);
            RefreshSide(Side.Enemy);
            hud.SetSynergies(Match);
            hud.SetCounts(Match.Player.Deck.Count, Match.Enemy.Hand.Count, Match.Enemy.Deck.Count);
            layout.playerDeck.SetCount(Match.Player.Deck.Count);
            layout.enemyDeck.SetCount(Match.Enemy.Deck.Count);
        }

        IEnumerator GameOverFlow(MatchResult result)
        {
            busy = true;
            yield return new WaitForSeconds(0.9f);
            bool victory = result == MatchResult.Victory;
            if (victory) enemyNexus.Collapse();
            else playerNexus.Collapse();
            if (cameraRig != null) cameraRig.Shake(0.3f, 0.9f);
            AudioManager.Fire(victory ? Sfx.Victory : Sfx.Defeat);
            if (AudioManager.Instance != null) AudioManager.Instance.DuckMusic(0.1f, 1.2f);
            yield return new WaitForSeconds(1.1f);
            int score = Match.Score.Total;
            int bestBefore = GameSettings.BestScore;
            bool record = GameSettings.SubmitScore(score);
            string details = "Turnos: " + Match.TurnNumber
                             + "    Torre: " + (Match.TurretHp <= 0 ? "destruída" : "de pé")
                             + "    Combos: " + playerCombos
                             + "\nPressione R para jogar novamente";
            hud.result.Show(victory, score, Mathf.Max(bestBefore, score), record, details);
        }
    }
}
