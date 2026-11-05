using System;
using Godot;

namespace CitUp.Game;

/// <summary>Окно «Соединение потеряно» поверх игры с кнопками «Переподключиться» и «В меню». Весь интерфейс собирается в коде.</summary>
public partial class ConnectionOverlay : CanvasLayer
{
    public event Action? ReconnectPressed;
    public event Action? MenuPressed;

    private Label _title = null!;
    private Label _text = null!;
    private Button _reconnect = null!;
    private Button _menu = null!;

    public override void _Ready()
    {
        Layer = 20;   // выше стартового меню (10) и интерфейса игры
        Visible = false;

        // Затемнение на весь экран; заодно не пускает клики к игре под окном
        var dim = new ColorRect { Color = new Color(0f, 0f, 0f, 0.6f) };
        dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(dim);

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer();
        center.AddChild(panel);

        var margin = new MarginContainer();
        foreach (var side in new[] { "margin_left", "margin_top", "margin_right", "margin_bottom" })
            margin.AddThemeConstantOverride(side, 20);
        panel.AddChild(margin);

        var box = new VBoxContainer { CustomMinimumSize = new Vector2(420f, 0f) };
        box.AddThemeConstantOverride("separation", 10);
        margin.AddChild(box);

        _title = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _title.AddThemeFontSizeOverride("font_size", 24);
        box.AddChild(_title);

        _text = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        box.AddChild(_text);

        _reconnect = new Button { Text = "Переподключиться" };
        _reconnect.Pressed += () => ReconnectPressed?.Invoke();
        box.AddChild(_reconnect);

        _menu = new Button { Text = "В меню" };
        _menu.Pressed += () => MenuPressed?.Invoke();
        box.AddChild(_menu);
    }

    /// <summary>Связь потеряна или подключиться не вышло: обе кнопки доступны.</summary>
    public void ShowLost(string text)
    {
        _title.Text = "Соединение потеряно";
        _text.Text = text;
        _reconnect.Disabled = false;
        _menu.Disabled = false;
        Visible = true;
    }

    /// <summary>Идёт попытка подключения: «Переподключиться» неактивна, выйти в меню можно.</summary>
    public void ShowBusy(string text)
    {
        _title.Text = "Переподключение";
        _text.Text = text;
        _reconnect.Disabled = true;
        _menu.Disabled = false;
        Visible = true;
    }

    public void Close() => Visible = false;
}