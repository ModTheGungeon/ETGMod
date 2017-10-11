using UnityEngine;

namespace SGUI {
    /// <summary>
    /// A SGUI backend. Only use it in a valid context (when rendering)!
    /// </summary>
    public interface ISGUIBackend {

        /// <summary>
        /// The root that currently uses the backend to render.
        /// </summary>
        /// <value>The current root using the backend. Null when not rendering.</value>
        SGUIRoot CurrentRoot { get; }

        bool UpdateStyleOnGUI { get; }

        /// <summary>
        /// Whether to use OnGUI (IMGUI) or not (GameObject-based / shadow hierarchy).
        /// </summary>
        /// <value><c>true</c> if the children of CurrentRoot should be rendered on and Start-/EndRender should get called in OnGUI.</value>
        bool RenderOnGUI { get; }
        /// <summary>
        /// Whether GUILayout is used in OnGUI or not. Returning false here cuts half the OnGUI calls, but disables GUILayout / GUI.Window.
        /// </summary>
        /// <value><c>true</c> if the backend uses GUILayout.</value>
        bool RenderOnGUILayout { get; }

        bool IsOnGUIRepainting { get; }

        float LineHeight { get; }
        float IconPadding { get; }
        object Font { get; set; }

        Vector2 ScrollBarSizes { get; }

        bool LastMouseEventConsumed { get; }
        bool LastKeyEventConsumed { get; }

        bool IsValidSecret(long secret);
        void VerifySecret(long secret);

        void Init();

        bool Initialized { get; }

        void StartOnGUI(SGUIRoot root);
        void EndOnGUI(SGUIRoot root);
        void OnGUI();

        int CurrentComponentID { get; }
        int GetFirstComponentID(SElement elem);
        int CurrentElementID { get; }
        int GetElementID(SElement elem);

        void Focus(SElement elem);
        void Focus(int id);
        bool IsFocused(SElement elem);
        bool IsFocused(int id);

        void Texture(SElement elem, Vector2 position, Vector2 size, Texture texture, Color? color = null);
        void Texture(Vector2 position, Vector2 size, Texture texture, Color? color = null);

        void Rect(SElement elem, Vector2 position, Vector2 size, Color color);
        void Rect(Vector2 position, Vector2 size, Color color);

        void StartClip(SElement elem);
        void StartClip(SElement elem, Rect bounds);
        void StartClip(Rect bounds);
        void EndClip();

        /// <summary>
        /// Render the specified text on screen.
        /// </summary>
        /// <param name="elem">Element instance. Null for root.</param>
        /// <param name="position">Relative position to render the text at.</param>
        /// <param name="size">Size in which the text should fit in.</param>
        /// <param name="text">Text to render.</param>
        /// <param name="alignment">How to align the text in the label.</param>
        /// <param name="icon">An optional icon for the label.</param>
        /// <param name="color">Color for the label.</param>
        void Text(SElement elem, Vector2 position, Vector2 size, string text, TextAnchor alignment = TextAnchor.MiddleCenter, Texture icon = null, Color? color = null);

        /// <summary>
        /// Render a text field on screen.
        /// </summary>
        /// <param name="elem">Element instance. Null for root.</param>
        /// <param name="position">Position.</param>
        /// <param name="size">Size.</param>
        /// <param name="text">Text.</param>
        void TextField(SElement elem, Vector2 position, Vector2 size, ref string text);
        /// <summary>
        /// Render a text field on screen.
        /// </summary>
        /// <param name="bounds">Bounds.</param>
        /// <param name="text">Text.</param>
        void TextField(Rect bounds, ref string text);
        void MoveTextFieldCursor(SElement elem, ref int? cursor, ref int? selection);

        void Button(SElement elem, Vector2 position, Vector2 size, string text, TextAnchor alignment = TextAnchor.MiddleCenter, Texture icon = null);

        void StartGroup(SGroup group);
        void EndGroup(SGroup group);

        void Window(SGroup group);
        void StartWindow(SGroup group);
        void EndWindow(SGroup group);
        void WindowTitleBar(SWindowTitleBar bar);

        /// <summary>
        /// Gets the size of the text.
        /// </summary>
        /// <returns>The size of the given text.</returns>
        /// <param name="text">The text to measure.</param>
        /// <param name="width">The width in which the text should fit in.</param>
        /// <param name="font">The font in which the text gets rendered.</param>
        Vector2 MeasureText(string text, float? width = null, object font = null);
        /// <summary>
        /// Gets the size of the text.
        /// </summary>
        /// <returns>The size of the given text.</returns>
        /// <param name="text">The text to measure. Will be adapted to fit in the given bounds.</param>
        /// <param name="width">The width in which the text should fit in.</param>
        /// <param name="font">The font in which the text gets rendered.</param>
        Vector2 MeasureText(ref string text, float? width = null, object font = null);

        // Text auto-generated by MonoDevelop. Nice! -- 0x0ade
        /// <summary>
        /// Releases all resource used by the <see cref="T:SGUI.ISGUIBackend"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose()"/> when you are finished using the <see cref="T:SGUI.ISGUIBackend"/>. The
        /// <see cref="Dispose()"/> method leaves the <see cref="T:SGUI.ISGUIBackend"/> in an unusable state. After
        /// calling <see cref="Dispose()"/>, you must release all references to the <see cref="T:SGUI.ISGUIBackend"/> so
        /// the garbage collector can reclaim the memory that the <see cref="T:SGUI.ISGUIBackend"/> was occupying.</remarks>
        void Dispose();

        /// <summary>
        /// Dispose the specified elem.
        /// </summary>
        /// <param name="elem">Element.</param>
        void Dispose(SElement elem);

    }
}
