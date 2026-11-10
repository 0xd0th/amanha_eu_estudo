using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;

public static class MenuPlaceholderBuilder
{
    static Font font;
    static RectTransform Box(string name, Transform parent, Vector2 size, Vector2 pos, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Create menu placeholders");
        var r = go.GetComponent<RectTransform>(); r.SetParent(parent, false);
        r.sizeDelta = size; r.anchoredPosition = pos;
        var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
        return r;
    }
    static void Label(string name, Transform parent, string value, int size, Vector2 dimensions, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        var r = go.GetComponent<RectTransform>(); r.SetParent(parent, false); r.sizeDelta=dimensions; r.anchoredPosition=pos;
        var t=go.GetComponent<Text>(); t.font=font; t.text=value; t.fontSize=size; t.alignment=TextAnchor.MiddleCenter; t.color=Color.white; t.raycastTarget=false;
    }
    static Button Button(string name, Transform parent, string title, Vector2 pos)
    {
        var r=Box(name,parent,new Vector2(270,65),pos,new Color(.26f,.30f,.40f));
        var b=r.gameObject.AddComponent<Button>(); b.targetGraphic=r.GetComponent<Image>(); b.targetGraphic.raycastTarget=true;
        Label("Label",r,title,24,new Vector2(260,60),Vector2.zero); return b;
    }
    [MenuItem("Tools/Game/Create Placeholder Menu")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("Stop Play mode first."); return; }
        var canvas=Object.FindFirstObjectByType<Canvas>();
        if(canvas==null) { Debug.LogError("Open the opening scene containing Canvas."); return; }
        var existing=canvas.transform.Find("MainMenuPanel");
        if(existing!=null && existing.Find("PhoneRoot")!=null) { Selection.activeGameObject=existing.gameObject; return; }
        font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Undo.IncrementCurrentGroup(); int group=Undo.GetCurrentGroup();
        var menu=existing as RectTransform;
        if(menu==null) menu=Box("MainMenuPanel",canvas.transform,Vector2.zero,Vector2.zero,Color.clear);
        Undo.RecordObject(menu,"Menu layout"); menu.anchorMin=Vector2.zero; menu.anchorMax=Vector2.one; menu.offsetMin=menu.offsetMax=Vector2.zero;
        var bg=Box("BackgroundImage",menu,Vector2.zero,Vector2.zero,new Color(.13f,.18f,.24f)); bg.anchorMin=Vector2.zero; bg.anchorMax=Vector2.one; bg.offsetMin=bg.offsetMax=Vector2.zero;
        var title=Box("GameTitleImage",menu,new Vector2(1000,130),Vector2.zero,Color.clear); title.anchorMin=title.anchorMax=new Vector2(.5f,1); title.anchoredPosition=new Vector2(0,-110);
        Label("TitlePlaceholder",title,"amanhã eu estudo",68,new Vector2(1000,130),Vector2.zero);
        var phone=Box("PhoneRoot",menu,new Vector2(430,760),Vector2.zero,Color.clear); phone.anchorMin=phone.anchorMax=new Vector2(.5f,0); phone.pivot=new Vector2(.5f,0); phone.anchoredPosition=new Vector2(0,0);
        Box("HandImage",phone,new Vector2(400,440),new Vector2(55,-160),new Color(.84f,.48f,.31f));
        Box("PhoneFrameImage",phone,new Vector2(350,680),new Vector2(0,20),new Color(.055f,.065f,.08f));
        var screen=Box("ScreenContent",phone,new Vector2(310,625),new Vector2(0,20),new Color(.18f,.21f,.29f));
        var home=Box("HomePage",screen,new Vector2(310,625),Vector2.zero,Color.clear);
        Label("DateText",home,"SEG. 1 ABRIL",22,new Vector2(290,45),new Vector2(0,245));
        Label("ClockText",home,"9:41",76,new Vector2(290,100),new Vector2(0,160));
        var start=Button("NewGameButton",home,"NOVO JOGO",new Vector2(0,35)); start.interactable=false;
        var cont=Button("ContinueButton",home,"CONTINUAR",new Vector2(0,-50)); cont.interactable=false;
        var settingsButton=Button("SettingsButton",home,"CONFIGURAÇÕES",new Vector2(0,-160));
        var quit=Button("QuitButton",home,"SAIR",new Vector2(0,-245)); quit.interactable=false;
        var settings=Box("SettingsPage",screen,new Vector2(310,625),Vector2.zero,new Color(.18f,.21f,.29f));
        Label("TitleText",settings,"CONFIGURAÇÕES",24,new Vector2(290,60),new Vector2(0,240));
        Label("VolumeLabel",settings,"volume",24,new Vector2(290,45),new Vector2(0,140));
        var track=Box("VolumeSlider",settings,new Vector2(250,20),new Vector2(0,90),new Color(.32f,.37f,.46f));
        var handle=Box("Handle",track,new Vector2(24,36),Vector2.zero,Color.white);
        var slider=track.gameObject.AddComponent<Slider>(); slider.handleRect=handle; slider.targetGraphic=handle.GetComponent<Image>(); handle.GetComponent<Image>().raycastTarget=true; slider.value=.7f; slider.interactable=false;
        Label("AccessibilityOptions",settings,"acessibilidade\n\ntamanho do texto\nreduzir animações",22,new Vector2(290,180),new Vector2(0,-55));
        var back=Button("BackButton",settings,"VOLTAR",new Vector2(0,-245));
        UnityEventTools.AddBoolPersistentListener(settingsButton.onClick,settings.gameObject.SetActive,true);
        UnityEventTools.AddBoolPersistentListener(settingsButton.onClick,home.gameObject.SetActive,false);
        UnityEventTools.AddBoolPersistentListener(back.onClick,home.gameObject.SetActive,true);
        UnityEventTools.AddBoolPersistentListener(back.onClick,settings.gameObject.SetActive,false);
        settings.gameObject.SetActive(false);
        var scaler=canvas.GetComponent<CanvasScaler>(); if(scaler!=null) { Undo.RecordObject(scaler,"Canvas scaling"); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f; }
        var oldSettings=canvas.transform.Find("SettingsPanel"); if(oldSettings!=null) { Undo.RecordObject(oldSettings.gameObject,"Hide old settings"); oldSettings.gameObject.SetActive(false); }
        Undo.RecordObject(menu.gameObject,"Show menu"); menu.gameObject.SetActive(true);
        var splash=canvas.transform.Find("SplashPanel"); if(splash!=null) { Undo.RecordObject(splash.gameObject,"Preview menu"); splash.gameObject.SetActive(false); }
        Selection.activeGameObject=menu.gameObject; Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("Placeholder menu created. Settings/back are connected; gameplay and volume controls are placeholders. Save the scene after reviewing.");
    }
}
