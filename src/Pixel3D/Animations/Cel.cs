using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Pixel3D.Animations
{
    [DebuggerDisplay("Cel:{EditorName}")]
    public class Cel : IEditorNameProvider
    {
        /// <summary>In theory, this should be null if we are not inside a layer list.</summary>
        public Cel next;

        /// <summary>IMPORTANT: Do not use in gameplay code (not network safe)</summary>
        [NonSerialized]
        public string friendlyName;

        public SpriteRef spriteRef;
        public ShadowReceiver shadowReceiver;


        public Cel(Sprite sprite)
        {
            this.spriteRef = new SpriteRef(sprite);
        }


        public string EditorName
        {
            get
            {
                string name = friendlyName ?? "(no name)";

                if(shadowReceiver != null)
                    name = name + " [shadow receiver]";

                return name;
            }
        }


        /// <summary>Calculate the world-space bounds of the Cel. EDITOR ONLY!</summary>
        public Rectangle CalculateGraphicsBounds()
        {
            return spriteRef.ResolveRequire().WorldSpaceBounds;
        }


        public void Draw(DrawContext drawContext, Position position, bool flipX, Color color)
        {
            Sprite sprite;
            if(!spriteRef.ResolveBestEffort(out sprite))
                return;

            if(shadowReceiver != null)
            {
                drawContext.DrawShadowReceiver(sprite, shadowReceiver, position, flipX);
            }
            else
            {
                drawContext.DrawWorld(sprite, position, color, flipX);
            }
        }

		#region Serialize

        public void Serialize(AnimationSerializeContext context)
        {
            context.bw.WriteNullableString(friendlyName);
            spriteRef.Serialize(context);

            if(context.bw.WriteBoolean(shadowReceiver != null))
            {
	            shadowReceiver?.Serialize(context);
            }
        }

        /// <summary>Deserialize into new object instance</summary>
        public Cel(AnimationDeserializeContext context)
        {
            friendlyName = context.br.ReadNullableString();
            spriteRef = new SpriteRef(context);

            if(context.br.ReadBoolean())
                shadowReceiver = new ShadowReceiver(context);
        }

        #endregion
    }
}