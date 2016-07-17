using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class ETGDamageIndicatorGUI : MonoBehaviour {

    private static List<DamageIndicator> _Indicators = new List<DamageIndicator>();
    private static List<DamageIndicator> _UsedPool = new List<DamageIndicator>();
    private static List<DamageIndicator> _ToRemove = new List<DamageIndicator>();

    private static Dictionary<HealthHaver, HealthBar> _AllBars = new Dictionary<HealthHaver, HealthBar>();
    public static Dictionary<HealthHaver, float> MaxHP = new Dictionary<HealthHaver, float>();
    public static Dictionary<HealthHaver, float> CurrentHP = new Dictionary<HealthHaver, float>();

    public static List<HealthHaver> ToRemoveBars = new List<HealthHaver>();

    public static bool RenderHealthBars = true;

    public static void Create() {
        GameObject newObject = new GameObject();
        newObject.name="Damage Indicators";
        newObject.AddComponent<ETGDamageIndicatorGUI>();
        newObject.transform.SetParent(ETGModGUI.MenuObject.transform);
    }

    public void Start() {
        DontDestroyOnLoad(gameObject);
    }

    public void Update() {
        foreach (DamageIndicator i in _Indicators)
            i.Update();

        foreach (DamageIndicator i in _ToRemove)
            _Indicators.Remove(i);

        try {
            foreach (HealthBar bar in _AllBars.Values)
                bar.Update();

            foreach (HealthHaver hh in ToRemoveBars) {
                _AllBars.Remove(hh);
            }

            _ToRemove.Clear();
            ToRemoveBars.Clear();
        }
        catch (System.Exception e) {
            Debug.Log(e.ToString());
        }
    }

    public void OnGUI() {
        foreach (DamageIndicator i in _Indicators)
            i.OnGUI();

        foreach (HealthBar bar in _AllBars.Values)
            bar.OnGUI();
    }

    public static void CreateIndicator(Vector3 worldPosOrigin, object content) {

        //We need to make a new object
        if (_UsedPool.Count==0) {
            DamageIndicator newIndicator = new DamageIndicator();

            newIndicator.WPosOrigin=worldPosOrigin;
            newIndicator.Content="<size=35>"+content+"</size>";

            _Indicators.Add(newIndicator);
        } else {
            //We have a pooled object, use this instead

            DamageIndicator pickedIndicator = _UsedPool[0];
            _UsedPool.RemoveAt(0);

            pickedIndicator.Content="<size=35>"+content+"</size>";
            pickedIndicator.WPosOrigin=worldPosOrigin;

            _Indicators.Add(pickedIndicator);
        }
    }

    public static void CreateBar(HealthHaver targ) {

        if (_AllBars.ContainsKey(targ))
            return;

        HealthBar newBar = new HealthBar();
        newBar.Target=targ;

        _AllBars.Add(targ, newBar);
    }

    public static void UpdateHealthBar(HealthHaver hh, float dmg) {
        if (_AllBars.ContainsKey(hh))
            _AllBars[hh].UpdateTransitionBar(dmg);
    }

    private class DamageIndicator {
        public Vector2 Offset;
        public Vector3 WPosOrigin;
        public object Content;

        private float _Time;

        public void Update() {
            Offset-=Vector2.up*Time.deltaTime*5;
            Offset=new Vector2(Mathf.Sin(_Time*15)-0.5f, Offset.y);
            _Time+=Time.deltaTime;

            if (_Time>=2) {
                _UsedPool.Add(this);
                _ToRemove.Add(this);
                _Time=0;
                Offset=Vector2.zero;
            }
        }

        public void OnGUI() {
            Vector2 worldToScreenPoint = Camera.main.WorldToScreenPoint(WPosOrigin);
            worldToScreenPoint=new Vector2(worldToScreenPoint.x, Screen.height-worldToScreenPoint.y);
            GUI.color=Color.red;
            GUI.Label(new Rect(worldToScreenPoint+( Offset*15 )+( new Vector2(25, 25) ), new Vector2(50, 50)), Content.ToStringIfNoString());
            GUI.color=Color.white;
        }
    }

    private class HealthBar {
        public HealthHaver Target;

        Rect OutlineRect = new Rect();
        Rect TotalBarRect = new Rect();
        Rect FilledBarRect = new Rect();
        Rect TransitionBarRect = new Rect();

        //The point on the HP bar we're transitioning to.
        float CurrentPoint = 1f;
        //The time before transition takes place.
        float TransitionDelay = 0.1f;
        float TransitionSpeed = 0.3f;

        public void Update() {

            if (Target==null) {
                ToRemoveBars.Add(Target);
                return;
            }

            if (TransitionDelay>0)
                TransitionDelay-=Time.deltaTime;
            else if (CurrentPoint>=CurrentHP[Target]/MaxHP[Target])
                CurrentPoint-=Time.deltaTime*TransitionSpeed;

        }

        public void UpdateTransitionBar(float damageDelta) {

            //Local hit point, the place we where at before we took damage
            float hitPointL = ( CurrentHP[Target]+damageDelta )/MaxHP[Target];

            //If the current hit point is greater than the local one, we'll leave it as-is. Otherwise we're going to set it to the current hit point.
            if (Mathf.Abs(CurrentPoint-hitPointL)<0.01f) {
                CurrentPoint=hitPointL;
            }

            TransitionDelay=0.1f;
        }

        public void OnGUI() {

            try {
                Vector3 wPos = (Vector3)Target.SpeculativeRigidbody_0.PixelCollider_0.UnitTopCenter;
                Vector2 screenPos = Camera.main.WorldToScreenPoint(wPos);
                screenPos=new Vector2(screenPos.x, Screen.height-screenPos.y);

                int hpMaxBar = (int)Mathf.Max(0, 15-( MaxHP[Target] ))+100;

                OutlineRect=new Rect(screenPos.x-( hpMaxBar/2 )-2, screenPos.y-12, hpMaxBar+4, 24);
                TotalBarRect=new Rect(screenPos.x-( hpMaxBar/2 ), screenPos.y-10, hpMaxBar, 20);
                FilledBarRect=new Rect(screenPos.x-( hpMaxBar/2 ), screenPos.y-10, hpMaxBar*( CurrentHP[Target]/MaxHP[Target] ), 20);
                TransitionBarRect=new Rect(screenPos.x-( hpMaxBar/2 ), screenPos.y-10, hpMaxBar*( CurrentPoint ), 20);

                GUI.color=new Color(0, 0, 0, 1);
                GUI.DrawTexture(OutlineRect, ETGModGUI.BoxTexture);
                GUI.color=new Color(151f/255f, 166f/255f, 170f/255f);
                GUI.DrawTexture(TotalBarRect, ETGModGUI.BoxTexture);
                GUI.color=Color.Lerp(new Color(229f/255f, 54f/255f, 23f/255f), new Color(77f/255f, 214f/255f, 80f/255f), CurrentHP[Target]/MaxHP[Target]);
                GUI.DrawTexture(FilledBarRect, ETGModGUI.BoxTexture);
                GUI.color=Color.Lerp(new Color(229f/255f, 54f/255f, 23f/255f), new Color(77f/255f, 214f/255f, 80f/255f), CurrentPoint)*0.5f;
                GUI.DrawTexture(TransitionBarRect, ETGModGUI.BoxTexture);
                GUI.color=Color.white;
            } catch {

            }
        }

    }

}

