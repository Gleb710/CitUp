using Godot;

namespace CitUp.Game;

/// <summary>WASD или стрелки двигают камеру, колесо мыши меняет масштаб.</summary>
public partial class CameraController : Camera2D
{
    private const float MoveSpeed = 700f;   // пикселей экрана в секунду при масштабе 1
    private const float MinZoom = 0.5f;
    private const float MaxZoom = 2f;
    private const float ZoomStep = 1.1f;

    private float _zoom = 1f;

    public override void _Process(double delta)
    {
        var direction = Vector2.Zero;
        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up)) direction.Y -= 1f;
        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down)) direction.Y += 1f;
        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left)) direction.X -= 1f;
        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right)) direction.X += 1f;

        if (direction != Vector2.Zero)
            Position += direction.Normalized() * (MoveSpeed / _zoom) * (float)delta;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true } mouse)
        {
            if (mouse.ButtonIndex == MouseButton.WheelUp)
                SetZoomLevel(_zoom * ZoomStep);
            else if (mouse.ButtonIndex == MouseButton.WheelDown)
                SetZoomLevel(_zoom / ZoomStep);
        }
    }

    private void SetZoomLevel(float value)
    {
        _zoom = Mathf.Clamp(value, MinZoom, MaxZoom);
        Zoom = new Vector2(_zoom, _zoom);   // у Camera2D больше значение = ближе к карте
    }
}