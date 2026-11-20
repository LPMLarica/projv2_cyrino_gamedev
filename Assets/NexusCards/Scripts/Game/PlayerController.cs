using Nexus.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nexus
{
    public class PlayerController : MonoBehaviour
    {
        public BattleDirector director;
        public HUDController hud;
        public Camera cam;
        public float rayDistance = 120f;

        enum State { Idle, CardSelected, Attacker, SpellTarget }

        State state;
        CardInstance selected;
        CardView hoverCard;
        StructureView hoverStructure;
        SlotMarker hoverSlot;
        bool pointerOverUI;

        Match M { get { return director.Match; } }

        void Update()
        {
            if (InputShim.KeyDown(GameKey.H)) hud.ToggleHelp();
            if (InputShim.KeyDown(GameKey.M) && AudioManager.Instance != null) AudioManager.Instance.ToggleMute();
            if (M == null) return;

            bool accepting = director.AcceptingInput;
            if (!accepting && state != State.Idle) Clear();

            UpdateHover();

            if (accepting)
            {
                int digit = InputShim.DigitPressed();
                if (InputShim.KeyDown(GameKey.Escape) || InputShim.RightClick) Clear();
                else if (InputShim.KeyDown(GameKey.Space)) { Clear(); director.RequestEndTurn(); }
                else if (digit >= 0) SelectHandIndex(digit);
                else if (InputShim.LeftClick && !pointerOverUI) Click();
            }

            ApplyHighlights(accepting);
            UpdateInfo();

            bool handHover = hoverCard != null && M.Player.Hand.Contains(hoverCard.Card);
            director.HoveredHandUid = handHover ? hoverCard.Card.Uid : -1;
            director.SelectedHandUid = selected != null && (state == State.CardSelected || state == State.SpellTarget) ? selected.Uid : -1;
        }

        void UpdateHover()
        {
            hoverCard = null;
            hoverStructure = null;
            hoverSlot = null;
            pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (pointerOverUI || cam == null) return;

            RaycastHit hit;
            var ray = cam.ScreenPointToRay(InputShim.MousePosition);
            if (!Physics.Raycast(ray, out hit, rayDistance, ~0, QueryTriggerInteraction.Collide)) return;

            hoverCard = hit.collider.GetComponentInParent<CardView>();
            if (hoverCard != null && (hoverCard.Card == null || hoverCard.CurrentMode == CardView.Mode.Dying)) hoverCard = null;
            if (hoverCard != null) return;

            hoverStructure = hit.collider.GetComponentInParent<StructureView>();
            if (hoverStructure != null) return;

            var barrier = hit.collider.GetComponentInParent<GateBarrier>();
            if (barrier != null)
            {
                hoverStructure = barrier.protectedStructure;
                return;
            }

            hoverSlot = hit.collider.GetComponentInParent<SlotMarker>();
        }

        bool InHand(CardInstance c) { return c != null && M.Player.Hand.Contains(c); }
        bool OwnUnit(CardInstance c) { return c != null && c.Owner == Side.Player && c.OnBoard; }
        bool EnemyUnit(CardInstance c) { return c != null && c.Owner == Side.Enemy && c.OnBoard; }

        void Click()
        {
            var hc = hoverCard != null ? hoverCard.Card : null;
            switch (state)
            {
                case State.Idle:
                    if (InHand(hc)) SelectHand(hc);
                    else if (OwnUnit(hc)) SelectAttacker(hc);
                    break;

                case State.CardSelected:
                    if (hc == selected) PlaySelected(-1);
                    else if (hoverSlot != null && hoverSlot.side == Side.Player && selected.IsUnit && M.Player.Board[hoverSlot.index] == null) PlaySelected(hoverSlot.index);
                    else if (InHand(hc)) SelectHand(hc);
                    else if (OwnUnit(hc)) SelectAttacker(hc);
                    else Clear();
                    break;

                case State.SpellTarget:
                    if (EnemyUnit(hc))
                    {
                        if (director.TryCastSpell(selected, TargetRef.Champion(hc))) Clear();
                    }
                    else if (InHand(hc) && hc != selected) SelectHand(hc);
                    else Clear();
                    break;

                case State.Attacker:
                    TargetRef target;
                    if (HoverTarget(out target))
                    {
                        if (director.TryAttack(selected, target)) Clear();
                    }
                    else if (OwnUnit(hc) && hc != selected) SelectAttacker(hc);
                    else if (InHand(hc)) SelectHand(hc);
                    else Clear();
                    break;
            }
        }

        bool HoverTarget(out TargetRef target)
        {
            target = default(TargetRef);
            if (hoverCard != null && EnemyUnit(hoverCard.Card))
            {
                target = TargetRef.Champion(hoverCard.Card);
                return true;
            }
            if (hoverStructure != null && hoverStructure.side == Side.Enemy && !hoverStructure.Destroyed)
            {
                target = hoverStructure.kind == TargetKind.Turret ? TargetRef.Turret() : TargetRef.NexusOf(Side.Enemy);
                return true;
            }
            return false;
        }

        void SelectHandIndex(int index)
        {
            var hand = M.Player.Hand;
            if (index >= hand.Count) return;
            var c = hand[index];
            if (c == selected && state == State.CardSelected) PlaySelected(-1);
            else SelectHand(c);
        }

        void SelectHand(CardInstance c)
        {
            string reason;
            if (!M.CanPlay(Side.Player, c, out reason))
            {
                hud.Toast(reason, UIStyle.Crimson);
                Clear();
                return;
            }
            AudioManager.Fire(Sfx.Click, 0.6f);
            selected = c;
            state = c.Def.NeedsTarget ? State.SpellTarget : State.CardSelected;
        }

        void SelectAttacker(CardInstance c)
        {
            if (!c.CanAttack)
            {
                string reason = c.IsStunned ? "Atordoado: não ataca neste turno."
                    : c.WaitingSummon ? "Recém-invocado: ataca a partir do próximo turno."
                    : "Este campeão já atacou neste turno.";
                hud.Toast(reason, UIStyle.Muted);
                Clear();
                return;
            }
            AudioManager.Fire(Sfx.Click, 0.6f);
            selected = c;
            state = State.Attacker;
        }

        void PlaySelected(int slot)
        {
            if (selected == null) return;
            if (director.TryPlayCard(selected, slot)) Clear();
        }

        public void Clear()
        {
            state = State.Idle;
            selected = null;
        }

        void ApplyHighlights(bool accepting)
        {
            string reason;
            bool blocked = false;
            foreach (var c in M.Player.Hand)
            {
                var v = director.ViewOf(c);
                if (v == null) continue;
                if (c == selected) v.SetHighlight(CardHighlight.Selected);
                else v.SetHighlight(accepting && M.CanPlay(Side.Player, c, out reason) ? CardHighlight.Playable : CardHighlight.None);
            }
            foreach (var c in M.Player.Units())
            {
                var v = director.ViewOf(c);
                if (v == null || v.CurrentMode != CardView.Mode.Board) continue;
                if (c == selected && state == State.Attacker) v.SetHighlight(CardHighlight.Selected);
                else v.SetHighlight(accepting && c.CanAttack ? CardHighlight.Ready : CardHighlight.None);
            }
            foreach (var c in M.Enemy.Units())
            {
                var v = director.ViewOf(c);
                if (v == null || v.CurrentMode != CardView.Mode.Board) continue;
                bool target = (state == State.Attacker && M.ValidateAttack(selected, TargetRef.Champion(c)))
                              || (state == State.SpellTarget && M.IsValidSpellTarget(selected, TargetRef.Champion(c)));
                v.SetHighlight(target ? CardHighlight.Target : CardHighlight.None);
            }

            bool turretOk = state == State.Attacker && M.ValidateAttack(selected, TargetRef.Turret(), out reason, out blocked);
            director.enemyTurret.SetTargetable(turretOk, UIStyle.Crimson);
            blocked = false;
            bool nexusOk = state == State.Attacker && M.ValidateAttack(selected, TargetRef.NexusOf(Side.Enemy), out reason, out blocked);
            director.enemyNexus.SetTargetable(nexusOk, blocked ? new Color(1f, 0.6f, 0.2f) : UIStyle.Crimson);

            bool placing = state == State.CardSelected && selected != null && selected.IsUnit;
            foreach (var s in director.layout.playerSlots)
            {
                if (!placing || M.Player.Board[s.index] != null) s.SetState(SlotState.Idle);
                else s.SetState(s == hoverSlot ? SlotState.Hover : SlotState.Available);
            }
        }

        void UpdateInfo()
        {
            string title = null;
            string body = null;
            if (hoverCard != null && hoverCard.Card != null) DescribeCard(hoverCard.Card, out title, out body);
            else if (hoverStructure != null) DescribeStructure(hoverStructure, out title, out body);
            else if (state == State.CardSelected && selected != null)
            {
                title = selected.Def.Name;
                body = selected.IsUnit ? "Clique num espaço livre da sua fileira (ou na carta de novo) para invocar." : "Clique na carta de novo para lançar.";
            }
            else if (state == State.SpellTarget && selected != null)
            {
                title = selected.Def.Name;
                body = "Escolha um campeão inimigo como alvo. Botão direito cancela.";
            }
            else if (state == State.Attacker && selected != null)
            {
                title = selected.Def.Name + " pronto para atacar";
                body = "Clique num alvo destacado: campeão, Torre ou Nexus.";
            }
            hud.SetInfo(title, body);
        }

        void DescribeCard(CardInstance c, out string title, out string body)
        {
            var d = c.Def;
            title = d.Name;
            string side = c.Owner == Side.Enemy ? "Inimigo · " : "";
            if (!d.IsUnit)
            {
                body = side + "Feitiço · custo " + d.Cost + "\n" + d.RulesText;
                return;
            }
            body = side + CardDatabase.RegionName(d.Region) + " · custo " + d.Cost + " · " + c.Attack + " ATQ / " + c.Health + " VIDA";
            if (d.Has(Keyword.Guardiao)) body += "\nGuardião: inimigos precisam derrotá-lo antes de atingir outros alvos.";
            if (d.Has(Keyword.Impeto)) body += "\nÍmpeto: pode atacar no turno em que entra.";
            if (SynergyRules.RegionHasBonus(d.Region))
                body += "\nSinergia (2+ de " + CardDatabase.RegionName(d.Region) + "): " + SynergyRules.RegionBonusText(d.Region);
            var combo = SynergyRules.ComboFor(d.Id);
            if (combo != null)
            {
                string partner = combo.A == d.Id ? combo.B : combo.A;
                var pd = CardDatabase.Find(partner);
                body += "\nCombo com " + (pd != null ? pd.Name : partner) + " — " + combo.Name + ": " + combo.Text;
            }
            if (c.IsStunned) body += "\nAtordoado neste turno.";
        }

        void DescribeStructure(StructureView s, out string title, out string body)
        {
            if (s.kind == TargetKind.Turret)
            {
                title = "Torre inimiga";
                body = s.Destroyed ? "Destruída." :
                    "Vida " + M.TurretHp + "/" + M.TurretMax + ". Ao fim de cada turno inimigo dispara " + M.TurretDamage +
                    " de dano no seu campeão mais forte. Destrua-a para abrir o Portão.";
                return;
            }
            if (s.side == Side.Player)
            {
                title = "Seu Nexus";
                body = "Vida " + M.Player.NexusHp + "/" + M.Player.NexusMax + ". Se chegar a zero, você perde.";
                return;
            }
            title = "Nexus inimigo";
            body = "Vida " + M.Enemy.NexusHp + "/" + M.Enemy.NexusMax + ". " +
                   (M.GateOpen ? "Portão aberto: ataque à vontade (Guardiões primeiro)." : "Protegido pelo Portão: ataques serão bloqueados fisicamente.");
        }
    }
}
