namespace MinorShift.Emuera.UI.Game.Image
{
	abstract class AContentItem
	{
		protected AContentItem(string name) { Name = name; }
		public readonly string Name;
		//public bool Enabled { get; protected set; }
		public abstract bool IsCreated { get; }
	}
}
