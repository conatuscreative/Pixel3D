using Pixel3D.Extensions;
using Pixel3D.FrameworkExtensions;

namespace Pixel3D.Engine.Levels
{
	public static class LevelSerializeContextExtensions
	{
		#region Serialize

		public static void Serialize(this Thing thing, LevelSerializeContext context)
		{
			context.WriteAnimationSet(thing.AnimationSet);

			context.bw.Write(thing.Position);
			context.bw.Write(thing.FacingLeft);

			context.bw.WriteNullableString(thing.overrideBehaviour);

			context.bw.Write(thing.includeInNavigation);

			// Properties
			{
				context.bw.Write(thing.properties.Count);
				foreach (var kvp in thing.properties)
				{
					context.bw.Write(kvp.Key);
					context.bw.Write(kvp.Value ?? string.Empty); // (null value should probably be blocked by editor, but being safe...)
				}
			}
		}

		/// <summary>Deserialize into new object instance</summary>
		public static Thing DeserializeThing(this LevelDeserializeContext context)
		{
			var animationSet = context.ReadAnimationSet();
			var position = context.br.ReadPosition();
			var facingLeft = context.br.ReadBoolean();

			var thing = new Thing(animationSet, position, facingLeft);
			thing.overrideBehaviour = context.br.ReadNullableString();
			thing.includeInNavigation = context.br.ReadBoolean();

			// Properties
			int count = context.br.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				thing.properties.Add(context.br.ReadString(), context.br.ReadString());
			}

			return thing;
		}

		#endregion
	}
}
