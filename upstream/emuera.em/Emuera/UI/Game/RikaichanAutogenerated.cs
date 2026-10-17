using System.Collections.Generic;

//This file is generated from deinflect.js from rikaichamp. Use rikaichan.Autogenerate() to generate it again.

namespace MinorShift.Emuera.GameView;

enum DeinflectReason
{
	PolitePastNegative = 0,
	PoliteNegative,
	PoliteVolitional,
	Chau,
	Sugiru,
	Nasai,
	PolitePast,
	Tara,
	Tari,
	Causative,
	PotentialOrPassive,
	Toku,
	Sou,
	Tai,
	Polite,
	Past,
	Negative,
	Passive,
	Ba,
	Volitional,
	Potential,
	CausativePassive,
	Te,
	Zu,
	Imperative,
	MasuStem,
	Adv,
	Noun,
	ImperativeNegative,
	Continuous,
	Ki,
	SuruNoun,
}

class DeinflectRule
{
	public byte[] from;
	public byte[] to;
	public ushort type;
	public DeinflectReason reason;
}

partial class Rikaichan
{
	DeinflectRule[] deinflectRuleData;

	string[] deinflectReasonStrings = [
			"PolitePastNegative",
			"PoliteNegative",
			"PoliteVolitional",
			"Chau",
			"Sugiru",
			"Nasai",
			"PolitePast",
			"Tara",
			"Tari",
			"Causative",
			"PotentialOrPassive",
			"Toku",
			"Sou",
			"Tai",
			"Polite",
			"Past",
			"Negative",
			"Passive",
			"Ba",
			"Volitional",
			"Potential",
			"CausativePassive",
			"Te",
			"Zu",
			"Imperative",
			"MasuStem",
			"Adv",
			"Noun",
			"ImperativeNegative",
			"Continuous",
			"Ki",
			"SuruNoun",
		];

	void InitData()
	{
		List<DeinflectRule> list = new(0x40); //TODO: no idea how this c# thing works, there is probably a better way.
		DeinflectRule next;
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いらっしゃいませんでした"),
			to = eucjp.GetBytes("いらっしゃる"),
			type = 640,
			reason = DeinflectReason.PolitePastNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("おっしゃいませんでした"),
			to = eucjp.GetBytes("おっしゃる"),
			type = 640,
			reason = DeinflectReason.PolitePastNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いらっしゃいました"),
			to = eucjp.GetBytes("いらっしゃる"),
			type = 640,
			reason = DeinflectReason.PolitePast,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("くありませんでした"),
			to = eucjp.GetBytes("い"),
			type = 1152,
			reason = DeinflectReason.PolitePastNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いらっしゃいます"),
			to = eucjp.GetBytes("いらっしゃる"),
			type = 640,
			reason = DeinflectReason.Polite,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("おっしゃいました"),
			to = eucjp.GetBytes("おっしゃる"),
			type = 640,
			reason = DeinflectReason.PolitePast,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("仰いませんでした"),
			to = eucjp.GetBytes("仰る"),
			type = 640,
			reason = DeinflectReason.PolitePastNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いませんでした"),
			to = eucjp.GetBytes("う"),
			type = 640,
			reason = DeinflectReason.PolitePastNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("おっしゃいます"),
			to = eucjp.GetBytes("おっしゃる"),
			type = 640,
			reason = DeinflectReason.Polite,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きませんでした"),
			to = eucjp.GetBytes("く"),
			type = 640,
			reason = DeinflectReason.PolitePastNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きませんでした"),
			to = eucjp.GetBytes("くる"),
			type = 2176,
			reason = DeinflectReason.PolitePastNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ぎませんでした"),
			to = eucjp.GetBytes("ぐ"),
			type = 640,
			reason = DeinflectReason.PolitePastNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しませんでした"),
			to = eucjp.GetBytes("す"),
			type = 640,
			reason = DeinflectReason.PolitePastNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しませんでした"),
			to = eucjp.GetBytes("する"),
			type = 4224,
			reason = DeinflectReason.PolitePastNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しませんでした"),
			to = eucjp.GetBytes("す"),
			type = 4224,
			reason = DeinflectReason.PolitePastNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ちませんでした"),
			to = eucjp.GetBytes("つ"),
			type = 640,
			reason = DeinflectReason.PolitePastNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("にませんでした"),
			to = eucjp.GetBytes("ぬ"),
			type = 640,
			reason = DeinflectReason.PolitePastNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("びませんでした"),
			to = eucjp.GetBytes("ぶ"),
			type = 640,
			reason = DeinflectReason.PolitePastNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("みませんでした"),
			to = eucjp.GetBytes("む"),
			type = 640,
			reason = DeinflectReason.PolitePastNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("りませんでした"),
			to = eucjp.GetBytes("る"),
			type = 640,
			reason = DeinflectReason.PolitePastNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いらっしゃい"),
			to = eucjp.GetBytes("いらっしゃる"),
			type = 640,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いらっしゃい"),
			to = eucjp.GetBytes("いらっしゃる"),
			type = 640,
			reason = DeinflectReason.Imperative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("くありません"),
			to = eucjp.GetBytes("い"),
			type = 1152,
			reason = DeinflectReason.PoliteNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ませんでした"),
			to = eucjp.GetBytes("る"),
			type = 2432,
			reason = DeinflectReason.PolitePastNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("のたもうたら"),
			to = eucjp.GetBytes("のたまう"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("のたもうたり"),
			to = eucjp.GetBytes("のたまう"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いましょう"),
			to = eucjp.GetBytes("う"),
			type = 640,
			reason = DeinflectReason.PoliteVolitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("仰いました"),
			to = eucjp.GetBytes("仰る"),
			type = 640,
			reason = DeinflectReason.PolitePast,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("おっしゃい"),
			to = eucjp.GetBytes("おっしゃる"),
			type = 640,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("おっしゃい"),
			to = eucjp.GetBytes("おっしゃる"),
			type = 640,
			reason = DeinflectReason.Imperative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きましょう"),
			to = eucjp.GetBytes("く"),
			type = 640,
			reason = DeinflectReason.PoliteVolitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きましょう"),
			to = eucjp.GetBytes("くる"),
			type = 2176,
			reason = DeinflectReason.PoliteVolitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ぎましょう"),
			to = eucjp.GetBytes("ぐ"),
			type = 640,
			reason = DeinflectReason.PoliteVolitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しましょう"),
			to = eucjp.GetBytes("す"),
			type = 640,
			reason = DeinflectReason.PoliteVolitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しましょう"),
			to = eucjp.GetBytes("する"),
			type = 4224,
			reason = DeinflectReason.PoliteVolitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しましょう"),
			to = eucjp.GetBytes("す"),
			type = 4224,
			reason = DeinflectReason.PoliteVolitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ちましょう"),
			to = eucjp.GetBytes("つ"),
			type = 640,
			reason = DeinflectReason.PoliteVolitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("にましょう"),
			to = eucjp.GetBytes("ぬ"),
			type = 640,
			reason = DeinflectReason.PoliteVolitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("のたもうた"),
			to = eucjp.GetBytes("のたまう"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("のたもうて"),
			to = eucjp.GetBytes("のたまう"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("びましょう"),
			to = eucjp.GetBytes("ぶ"),
			type = 640,
			reason = DeinflectReason.PoliteVolitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("みましょう"),
			to = eucjp.GetBytes("む"),
			type = 640,
			reason = DeinflectReason.PoliteVolitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("りましょう"),
			to = eucjp.GetBytes("る"),
			type = 640,
			reason = DeinflectReason.PoliteVolitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いじゃう"),
			to = eucjp.GetBytes("ぐ"),
			type = 514,
			reason = DeinflectReason.Chau,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いすぎる"),
			to = eucjp.GetBytes("う"),
			type = 513,
			reason = DeinflectReason.Sugiru,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いちゃう"),
			to = eucjp.GetBytes("く"),
			type = 514,
			reason = DeinflectReason.Chau,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いったら"),
			to = eucjp.GetBytes("いく"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いったり"),
			to = eucjp.GetBytes("いく"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いている"),
			to = eucjp.GetBytes("く"),
			type = 513,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いでいる"),
			to = eucjp.GetBytes("ぐ"),
			type = 513,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いなさい"),
			to = eucjp.GetBytes("う"),
			type = 640,
			reason = DeinflectReason.Nasai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いました"),
			to = eucjp.GetBytes("う"),
			type = 640,
			reason = DeinflectReason.PolitePast,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いません"),
			to = eucjp.GetBytes("う"),
			type = 640,
			reason = DeinflectReason.PoliteNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("おうたら"),
			to = eucjp.GetBytes("おう"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("おうたり"),
			to = eucjp.GetBytes("おう"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("仰います"),
			to = eucjp.GetBytes("仰る"),
			type = 640,
			reason = DeinflectReason.Polite,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("かされる"),
			to = eucjp.GetBytes("く"),
			type = 513,
			reason = DeinflectReason.CausativePassive,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("かったら"),
			to = eucjp.GetBytes("い"),
			type = 1152,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("かったり"),
			to = eucjp.GetBytes("い"),
			type = 1152,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("がされる"),
			to = eucjp.GetBytes("ぐ"),
			type = 513,
			reason = DeinflectReason.CausativePassive,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きすぎる"),
			to = eucjp.GetBytes("く"),
			type = 513,
			reason = DeinflectReason.Sugiru,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きすぎる"),
			to = eucjp.GetBytes("くる"),
			type = 2049,
			reason = DeinflectReason.Sugiru,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ぎすぎる"),
			to = eucjp.GetBytes("ぐ"),
			type = 513,
			reason = DeinflectReason.Sugiru,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きちゃう"),
			to = eucjp.GetBytes("くる"),
			type = 2050,
			reason = DeinflectReason.Chau,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きている"),
			to = eucjp.GetBytes("くる"),
			type = 2049,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きなさい"),
			to = eucjp.GetBytes("く"),
			type = 640,
			reason = DeinflectReason.Nasai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きなさい"),
			to = eucjp.GetBytes("くる"),
			type = 2176,
			reason = DeinflectReason.Nasai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ぎなさい"),
			to = eucjp.GetBytes("ぐ"),
			type = 640,
			reason = DeinflectReason.Nasai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きました"),
			to = eucjp.GetBytes("く"),
			type = 640,
			reason = DeinflectReason.PolitePast,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きました"),
			to = eucjp.GetBytes("くる"),
			type = 2176,
			reason = DeinflectReason.PolitePast,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ぎました"),
			to = eucjp.GetBytes("ぐ"),
			type = 640,
			reason = DeinflectReason.PolitePast,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きません"),
			to = eucjp.GetBytes("く"),
			type = 640,
			reason = DeinflectReason.PoliteNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きません"),
			to = eucjp.GetBytes("くる"),
			type = 2176,
			reason = DeinflectReason.PoliteNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ぎません"),
			to = eucjp.GetBytes("ぐ"),
			type = 640,
			reason = DeinflectReason.PoliteNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("こうたら"),
			to = eucjp.GetBytes("こう"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("こうたり"),
			to = eucjp.GetBytes("こう"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("こさせる"),
			to = eucjp.GetBytes("くる"),
			type = 2049,
			reason = DeinflectReason.Causative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("こられる"),
			to = eucjp.GetBytes("くる"),
			type = 2049,
			reason = DeinflectReason.PotentialOrPassive,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しすぎる"),
			to = eucjp.GetBytes("す"),
			type = 4609,
			reason = DeinflectReason.Sugiru,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しすぎる"),
			to = eucjp.GetBytes("する"),
			type = 4097,
			reason = DeinflectReason.Sugiru,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しちゃう"),
			to = eucjp.GetBytes("す"),
			type = 4610,
			reason = DeinflectReason.Chau,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しちゃう"),
			to = eucjp.GetBytes("する"),
			type = 4098,
			reason = DeinflectReason.Chau,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("している"),
			to = eucjp.GetBytes("す"),
			type = 4609,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("している"),
			to = eucjp.GetBytes("する"),
			type = 4097,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しなさい"),
			to = eucjp.GetBytes("す"),
			type = 4736,
			reason = DeinflectReason.Nasai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しなさい"),
			to = eucjp.GetBytes("する"),
			type = 4224,
			reason = DeinflectReason.Nasai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しました"),
			to = eucjp.GetBytes("す"),
			type = 4736,
			reason = DeinflectReason.PolitePast,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しました"),
			to = eucjp.GetBytes("する"),
			type = 4224,
			reason = DeinflectReason.PolitePast,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しません"),
			to = eucjp.GetBytes("す"),
			type = 4736,
			reason = DeinflectReason.PoliteNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しません"),
			to = eucjp.GetBytes("する"),
			type = 4224,
			reason = DeinflectReason.PoliteNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("そうたら"),
			to = eucjp.GetBytes("そう"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("そうたり"),
			to = eucjp.GetBytes("そう"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("たされる"),
			to = eucjp.GetBytes("つ"),
			type = 513,
			reason = DeinflectReason.CausativePassive,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ちすぎる"),
			to = eucjp.GetBytes("つ"),
			type = 513,
			reason = DeinflectReason.Sugiru,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ちなさい"),
			to = eucjp.GetBytes("つ"),
			type = 640,
			reason = DeinflectReason.Nasai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ちました"),
			to = eucjp.GetBytes("つ"),
			type = 640,
			reason = DeinflectReason.PolitePast,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ちません"),
			to = eucjp.GetBytes("つ"),
			type = 640,
			reason = DeinflectReason.PoliteNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("っちゃう"),
			to = eucjp.GetBytes("う"),
			type = 514,
			reason = DeinflectReason.Chau,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("っちゃう"),
			to = eucjp.GetBytes("く"),
			type = 514,
			reason = DeinflectReason.Chau,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("っちゃう"),
			to = eucjp.GetBytes("つ"),
			type = 514,
			reason = DeinflectReason.Chau,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("っちゃう"),
			to = eucjp.GetBytes("る"),
			type = 514,
			reason = DeinflectReason.Chau,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("っている"),
			to = eucjp.GetBytes("う"),
			type = 513,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("っている"),
			to = eucjp.GetBytes("つ"),
			type = 513,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("っている"),
			to = eucjp.GetBytes("る"),
			type = 513,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("とうたら"),
			to = eucjp.GetBytes("とう"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("とうたり"),
			to = eucjp.GetBytes("とう"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("なされる"),
			to = eucjp.GetBytes("ぬ"),
			type = 513,
			reason = DeinflectReason.CausativePassive,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("にすぎる"),
			to = eucjp.GetBytes("ぬ"),
			type = 513,
			reason = DeinflectReason.Sugiru,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("になさい"),
			to = eucjp.GetBytes("ぬ"),
			type = 640,
			reason = DeinflectReason.Nasai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("にました"),
			to = eucjp.GetBytes("ぬ"),
			type = 640,
			reason = DeinflectReason.PolitePast,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("にません"),
			to = eucjp.GetBytes("ぬ"),
			type = 640,
			reason = DeinflectReason.PoliteNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ばされる"),
			to = eucjp.GetBytes("ぶ"),
			type = 513,
			reason = DeinflectReason.CausativePassive,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("びすぎる"),
			to = eucjp.GetBytes("ぶ"),
			type = 513,
			reason = DeinflectReason.Sugiru,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("びなさい"),
			to = eucjp.GetBytes("ぶ"),
			type = 640,
			reason = DeinflectReason.Nasai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("びました"),
			to = eucjp.GetBytes("ぶ"),
			type = 640,
			reason = DeinflectReason.PolitePast,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("びません"),
			to = eucjp.GetBytes("ぶ"),
			type = 640,
			reason = DeinflectReason.PoliteNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("まされる"),
			to = eucjp.GetBytes("む"),
			type = 513,
			reason = DeinflectReason.CausativePassive,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ましょう"),
			to = eucjp.GetBytes("る"),
			type = 2432,
			reason = DeinflectReason.PoliteVolitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("みすぎる"),
			to = eucjp.GetBytes("む"),
			type = 513,
			reason = DeinflectReason.Sugiru,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("みなさい"),
			to = eucjp.GetBytes("む"),
			type = 640,
			reason = DeinflectReason.Nasai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("みました"),
			to = eucjp.GetBytes("む"),
			type = 640,
			reason = DeinflectReason.PolitePast,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("みません"),
			to = eucjp.GetBytes("む"),
			type = 640,
			reason = DeinflectReason.PoliteNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("らされる"),
			to = eucjp.GetBytes("る"),
			type = 513,
			reason = DeinflectReason.CausativePassive,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("りすぎる"),
			to = eucjp.GetBytes("る"),
			type = 513,
			reason = DeinflectReason.Sugiru,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("りなさい"),
			to = eucjp.GetBytes("る"),
			type = 640,
			reason = DeinflectReason.Nasai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("りました"),
			to = eucjp.GetBytes("る"),
			type = 640,
			reason = DeinflectReason.PolitePast,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("りません"),
			to = eucjp.GetBytes("る"),
			type = 640,
			reason = DeinflectReason.PoliteNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("わされる"),
			to = eucjp.GetBytes("う"),
			type = 513,
			reason = DeinflectReason.CausativePassive,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んじゃう"),
			to = eucjp.GetBytes("ぬ"),
			type = 514,
			reason = DeinflectReason.Chau,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んじゃう"),
			to = eucjp.GetBytes("ぶ"),
			type = 514,
			reason = DeinflectReason.Chau,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んじゃう"),
			to = eucjp.GetBytes("む"),
			type = 514,
			reason = DeinflectReason.Chau,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んでいる"),
			to = eucjp.GetBytes("ぬ"),
			type = 513,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んでいる"),
			to = eucjp.GetBytes("ぶ"),
			type = 513,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んでいる"),
			to = eucjp.GetBytes("む"),
			type = 513,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("行ったら"),
			to = eucjp.GetBytes("行く"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("行ったり"),
			to = eucjp.GetBytes("行く"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("逝ったら"),
			to = eucjp.GetBytes("逝く"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("逝ったり"),
			to = eucjp.GetBytes("逝く"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("往ったら"),
			to = eucjp.GetBytes("往く"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("往ったり"),
			to = eucjp.GetBytes("往く"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("逝ったら"),
			to = eucjp.GetBytes("逝く"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("逝ったり"),
			to = eucjp.GetBytes("逝く"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("往ったら"),
			to = eucjp.GetBytes("往く"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("往ったり"),
			to = eucjp.GetBytes("往く"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("請うたら"),
			to = eucjp.GetBytes("請う"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("請うたり"),
			to = eucjp.GetBytes("請う"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("乞うたら"),
			to = eucjp.GetBytes("乞う"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("乞うたり"),
			to = eucjp.GetBytes("乞う"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("恋うたら"),
			to = eucjp.GetBytes("恋う"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("恋うたり"),
			to = eucjp.GetBytes("恋う"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("来させる"),
			to = eucjp.GetBytes("来る"),
			type = 2049,
			reason = DeinflectReason.Causative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("來させる"),
			to = eucjp.GetBytes("來る"),
			type = 2049,
			reason = DeinflectReason.Causative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("来ました"),
			to = eucjp.GetBytes("来る"),
			type = 2176,
			reason = DeinflectReason.PolitePast,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("来ません"),
			to = eucjp.GetBytes("来る"),
			type = 2176,
			reason = DeinflectReason.PoliteNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("來ました"),
			to = eucjp.GetBytes("來る"),
			type = 2176,
			reason = DeinflectReason.PolitePast,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("來ません"),
			to = eucjp.GetBytes("來る"),
			type = 2176,
			reason = DeinflectReason.PoliteNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("来られる"),
			to = eucjp.GetBytes("来る"),
			type = 2049,
			reason = DeinflectReason.PotentialOrPassive,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("來られる"),
			to = eucjp.GetBytes("來る"),
			type = 2049,
			reason = DeinflectReason.PotentialOrPassive,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("問うたら"),
			to = eucjp.GetBytes("問う"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("問うたり"),
			to = eucjp.GetBytes("問う"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("負うたら"),
			to = eucjp.GetBytes("負う"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("負うたり"),
			to = eucjp.GetBytes("負う"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("沿うたら"),
			to = eucjp.GetBytes("沿う"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("沿うたり"),
			to = eucjp.GetBytes("沿う"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("添うたら"),
			to = eucjp.GetBytes("添う"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("添うたり"),
			to = eucjp.GetBytes("添う"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("副うたら"),
			to = eucjp.GetBytes("副う"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("副うたり"),
			to = eucjp.GetBytes("副う"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("厭うたら"),
			to = eucjp.GetBytes("厭う"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("厭うたり"),
			to = eucjp.GetBytes("厭う"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いそう"),
			to = eucjp.GetBytes("う"),
			type = 640,
			reason = DeinflectReason.Sou,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いたい"),
			to = eucjp.GetBytes("う"),
			type = 516,
			reason = DeinflectReason.Tai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いたら"),
			to = eucjp.GetBytes("く"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いだら"),
			to = eucjp.GetBytes("ぐ"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いたり"),
			to = eucjp.GetBytes("く"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いだり"),
			to = eucjp.GetBytes("ぐ"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いった"),
			to = eucjp.GetBytes("いく"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いって"),
			to = eucjp.GetBytes("いく"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いてる"),
			to = eucjp.GetBytes("く"),
			type = 513,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いでる"),
			to = eucjp.GetBytes("ぐ"),
			type = 513,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いとく"),
			to = eucjp.GetBytes("く"),
			type = 514,
			reason = DeinflectReason.Toku,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いどく"),
			to = eucjp.GetBytes("ぐ"),
			type = 514,
			reason = DeinflectReason.Toku,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("います"),
			to = eucjp.GetBytes("う"),
			type = 640,
			reason = DeinflectReason.Polite,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("おうた"),
			to = eucjp.GetBytes("おう"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("おうて"),
			to = eucjp.GetBytes("おう"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("かせる"),
			to = eucjp.GetBytes("く"),
			type = 513,
			reason = DeinflectReason.Causative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("がせる"),
			to = eucjp.GetBytes("ぐ"),
			type = 513,
			reason = DeinflectReason.Causative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("かった"),
			to = eucjp.GetBytes("い"),
			type = 1152,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("かない"),
			to = eucjp.GetBytes("く"),
			type = 516,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("がない"),
			to = eucjp.GetBytes("ぐ"),
			type = 516,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("かれる"),
			to = eucjp.GetBytes("く"),
			type = 513,
			reason = DeinflectReason.Passive,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("がれる"),
			to = eucjp.GetBytes("ぐ"),
			type = 513,
			reason = DeinflectReason.Passive,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きそう"),
			to = eucjp.GetBytes("く"),
			type = 640,
			reason = DeinflectReason.Sou,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きそう"),
			to = eucjp.GetBytes("くる"),
			type = 2176,
			reason = DeinflectReason.Sou,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ぎそう"),
			to = eucjp.GetBytes("ぐ"),
			type = 640,
			reason = DeinflectReason.Sou,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きたい"),
			to = eucjp.GetBytes("く"),
			type = 516,
			reason = DeinflectReason.Tai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きたい"),
			to = eucjp.GetBytes("くる"),
			type = 2052,
			reason = DeinflectReason.Tai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ぎたい"),
			to = eucjp.GetBytes("ぐ"),
			type = 516,
			reason = DeinflectReason.Tai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きたら"),
			to = eucjp.GetBytes("くる"),
			type = 2176,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きたり"),
			to = eucjp.GetBytes("くる"),
			type = 2176,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きてる"),
			to = eucjp.GetBytes("くる"),
			type = 2049,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きとく"),
			to = eucjp.GetBytes("くる"),
			type = 2050,
			reason = DeinflectReason.Toku,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きます"),
			to = eucjp.GetBytes("く"),
			type = 640,
			reason = DeinflectReason.Polite,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きます"),
			to = eucjp.GetBytes("くる"),
			type = 2176,
			reason = DeinflectReason.Polite,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ぎます"),
			to = eucjp.GetBytes("ぐ"),
			type = 640,
			reason = DeinflectReason.Polite,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("くない"),
			to = eucjp.GetBytes("い"),
			type = 1028,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ければ"),
			to = eucjp.GetBytes("い"),
			type = 1152,
			reason = DeinflectReason.Ba,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("こうた"),
			to = eucjp.GetBytes("こう"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("こうて"),
			to = eucjp.GetBytes("こう"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("こない"),
			to = eucjp.GetBytes("くる"),
			type = 2052,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("こよう"),
			to = eucjp.GetBytes("くる"),
			type = 2176,
			reason = DeinflectReason.Volitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("これる"),
			to = eucjp.GetBytes("くる"),
			type = 2049,
			reason = DeinflectReason.Potential,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("来れる"),
			to = eucjp.GetBytes("来る"),
			type = 2049,
			reason = DeinflectReason.Potential,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("來れる"),
			to = eucjp.GetBytes("來る"),
			type = 2049,
			reason = DeinflectReason.Potential,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("させる"),
			to = eucjp.GetBytes("する"),
			type = 4097,
			reason = DeinflectReason.Causative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("させる"),
			to = eucjp.GetBytes("る"),
			type = 257,
			reason = DeinflectReason.Causative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("させる"),
			to = eucjp.GetBytes("す"),
			type = 4609,
			reason = DeinflectReason.Causative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("さない"),
			to = eucjp.GetBytes("す"),
			type = 516,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("される"),
			to = eucjp.GetBytes("す"),
			type = 4609,
			reason = DeinflectReason.Passive,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("される"),
			to = eucjp.GetBytes("する"),
			type = 4097,
			reason = DeinflectReason.Passive,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しそう"),
			to = eucjp.GetBytes("す"),
			type = 4736,
			reason = DeinflectReason.Sou,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しそう"),
			to = eucjp.GetBytes("する"),
			type = 4224,
			reason = DeinflectReason.Sou,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("したい"),
			to = eucjp.GetBytes("す"),
			type = 4612,
			reason = DeinflectReason.Tai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("したい"),
			to = eucjp.GetBytes("する"),
			type = 4100,
			reason = DeinflectReason.Tai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("したら"),
			to = eucjp.GetBytes("す"),
			type = 4736,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("したら"),
			to = eucjp.GetBytes("する"),
			type = 4224,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("したり"),
			to = eucjp.GetBytes("す"),
			type = 4736,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("したり"),
			to = eucjp.GetBytes("する"),
			type = 4224,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("してる"),
			to = eucjp.GetBytes("す"),
			type = 4609,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("してる"),
			to = eucjp.GetBytes("する"),
			type = 4097,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しとく"),
			to = eucjp.GetBytes("す"),
			type = 4610,
			reason = DeinflectReason.Toku,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しとく"),
			to = eucjp.GetBytes("する"),
			type = 4098,
			reason = DeinflectReason.Toku,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しない"),
			to = eucjp.GetBytes("する"),
			type = 4100,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("します"),
			to = eucjp.GetBytes("す"),
			type = 4736,
			reason = DeinflectReason.Polite,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("します"),
			to = eucjp.GetBytes("する"),
			type = 4224,
			reason = DeinflectReason.Polite,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しよう"),
			to = eucjp.GetBytes("する"),
			type = 4224,
			reason = DeinflectReason.Volitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("すぎる"),
			to = eucjp.GetBytes("い"),
			type = 1025,
			reason = DeinflectReason.Sugiru,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("すぎる"),
			to = eucjp.GetBytes("る"),
			type = 2305,
			reason = DeinflectReason.Sugiru,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("そうた"),
			to = eucjp.GetBytes("そう"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("そうて"),
			to = eucjp.GetBytes("そう"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("たせる"),
			to = eucjp.GetBytes("つ"),
			type = 513,
			reason = DeinflectReason.Causative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("たない"),
			to = eucjp.GetBytes("つ"),
			type = 516,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("たれる"),
			to = eucjp.GetBytes("つ"),
			type = 513,
			reason = DeinflectReason.Passive,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ちそう"),
			to = eucjp.GetBytes("つ"),
			type = 640,
			reason = DeinflectReason.Sou,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ちたい"),
			to = eucjp.GetBytes("つ"),
			type = 516,
			reason = DeinflectReason.Tai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ちます"),
			to = eucjp.GetBytes("つ"),
			type = 640,
			reason = DeinflectReason.Polite,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ちゃう"),
			to = eucjp.GetBytes("る"),
			type = 2306,
			reason = DeinflectReason.Chau,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ったら"),
			to = eucjp.GetBytes("う"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ったら"),
			to = eucjp.GetBytes("つ"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ったら"),
			to = eucjp.GetBytes("る"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ったり"),
			to = eucjp.GetBytes("う"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ったり"),
			to = eucjp.GetBytes("つ"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ったり"),
			to = eucjp.GetBytes("る"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ってる"),
			to = eucjp.GetBytes("う"),
			type = 513,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ってる"),
			to = eucjp.GetBytes("つ"),
			type = 513,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ってる"),
			to = eucjp.GetBytes("る"),
			type = 513,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("っとく"),
			to = eucjp.GetBytes("う"),
			type = 514,
			reason = DeinflectReason.Toku,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("っとく"),
			to = eucjp.GetBytes("つ"),
			type = 514,
			reason = DeinflectReason.Toku,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("っとく"),
			to = eucjp.GetBytes("る"),
			type = 514,
			reason = DeinflectReason.Toku,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ている"),
			to = eucjp.GetBytes("る"),
			type = 2305,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("とうた"),
			to = eucjp.GetBytes("とう"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("とうて"),
			to = eucjp.GetBytes("とう"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("なさい"),
			to = eucjp.GetBytes("る"),
			type = 2432,
			reason = DeinflectReason.Nasai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("なせる"),
			to = eucjp.GetBytes("ぬ"),
			type = 513,
			reason = DeinflectReason.Causative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("なない"),
			to = eucjp.GetBytes("ぬ"),
			type = 516,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("なれる"),
			to = eucjp.GetBytes("ぬ"),
			type = 513,
			reason = DeinflectReason.Passive,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("にそう"),
			to = eucjp.GetBytes("ぬ"),
			type = 640,
			reason = DeinflectReason.Sou,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("にたい"),
			to = eucjp.GetBytes("ぬ"),
			type = 516,
			reason = DeinflectReason.Tai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("にます"),
			to = eucjp.GetBytes("ぬ"),
			type = 640,
			reason = DeinflectReason.Polite,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ばせる"),
			to = eucjp.GetBytes("ぶ"),
			type = 513,
			reason = DeinflectReason.Causative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ばない"),
			to = eucjp.GetBytes("ぶ"),
			type = 516,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ばれる"),
			to = eucjp.GetBytes("ぶ"),
			type = 513,
			reason = DeinflectReason.Passive,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("びそう"),
			to = eucjp.GetBytes("ぶ"),
			type = 640,
			reason = DeinflectReason.Sou,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("びたい"),
			to = eucjp.GetBytes("ぶ"),
			type = 516,
			reason = DeinflectReason.Tai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("びます"),
			to = eucjp.GetBytes("ぶ"),
			type = 640,
			reason = DeinflectReason.Polite,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ました"),
			to = eucjp.GetBytes("る"),
			type = 384,
			reason = DeinflectReason.PolitePast,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ませる"),
			to = eucjp.GetBytes("む"),
			type = 513,
			reason = DeinflectReason.Causative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ません"),
			to = eucjp.GetBytes("る"),
			type = 384,
			reason = DeinflectReason.PoliteNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("まない"),
			to = eucjp.GetBytes("む"),
			type = 516,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("まれる"),
			to = eucjp.GetBytes("む"),
			type = 513,
			reason = DeinflectReason.Passive,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("みそう"),
			to = eucjp.GetBytes("む"),
			type = 640,
			reason = DeinflectReason.Sou,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("みたい"),
			to = eucjp.GetBytes("む"),
			type = 516,
			reason = DeinflectReason.Tai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("みます"),
			to = eucjp.GetBytes("む"),
			type = 640,
			reason = DeinflectReason.Polite,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("らせる"),
			to = eucjp.GetBytes("る"),
			type = 513,
			reason = DeinflectReason.Causative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("らない"),
			to = eucjp.GetBytes("る"),
			type = 516,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("られる"),
			to = eucjp.GetBytes("る"),
			type = 2305,
			reason = DeinflectReason.PotentialOrPassive,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("られる"),
			to = eucjp.GetBytes("る"),
			type = 513,
			reason = DeinflectReason.Passive,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("りそう"),
			to = eucjp.GetBytes("る"),
			type = 640,
			reason = DeinflectReason.Sou,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("りたい"),
			to = eucjp.GetBytes("る"),
			type = 516,
			reason = DeinflectReason.Tai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ります"),
			to = eucjp.GetBytes("る"),
			type = 640,
			reason = DeinflectReason.Polite,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("わせる"),
			to = eucjp.GetBytes("う"),
			type = 513,
			reason = DeinflectReason.Causative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("わない"),
			to = eucjp.GetBytes("う"),
			type = 516,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("われる"),
			to = eucjp.GetBytes("う"),
			type = 513,
			reason = DeinflectReason.Passive,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んだら"),
			to = eucjp.GetBytes("ぬ"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んだら"),
			to = eucjp.GetBytes("ぶ"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んだら"),
			to = eucjp.GetBytes("む"),
			type = 640,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んだり"),
			to = eucjp.GetBytes("ぬ"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んだり"),
			to = eucjp.GetBytes("ぶ"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んだり"),
			to = eucjp.GetBytes("む"),
			type = 640,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んでる"),
			to = eucjp.GetBytes("ぬ"),
			type = 513,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んでる"),
			to = eucjp.GetBytes("ぶ"),
			type = 513,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んでる"),
			to = eucjp.GetBytes("む"),
			type = 513,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んどく"),
			to = eucjp.GetBytes("ぬ"),
			type = 514,
			reason = DeinflectReason.Toku,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んどく"),
			to = eucjp.GetBytes("ぶ"),
			type = 514,
			reason = DeinflectReason.Toku,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んどく"),
			to = eucjp.GetBytes("む"),
			type = 514,
			reason = DeinflectReason.Toku,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("行った"),
			to = eucjp.GetBytes("行く"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("行って"),
			to = eucjp.GetBytes("行く"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("逝った"),
			to = eucjp.GetBytes("逝く"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("逝って"),
			to = eucjp.GetBytes("逝く"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("往った"),
			to = eucjp.GetBytes("往く"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("往って"),
			to = eucjp.GetBytes("往く"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("請うた"),
			to = eucjp.GetBytes("請う"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("請うて"),
			to = eucjp.GetBytes("請う"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("乞うた"),
			to = eucjp.GetBytes("乞う"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("乞うて"),
			to = eucjp.GetBytes("乞う"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("恋うた"),
			to = eucjp.GetBytes("恋う"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("恋うて"),
			to = eucjp.GetBytes("恋う"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("問うた"),
			to = eucjp.GetBytes("問う"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("問うて"),
			to = eucjp.GetBytes("問う"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("負うた"),
			to = eucjp.GetBytes("負う"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("負うて"),
			to = eucjp.GetBytes("負う"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("沿うた"),
			to = eucjp.GetBytes("沿う"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("沿うて"),
			to = eucjp.GetBytes("沿う"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("添うた"),
			to = eucjp.GetBytes("添う"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("添うて"),
			to = eucjp.GetBytes("添う"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("副うた"),
			to = eucjp.GetBytes("副う"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("副うて"),
			to = eucjp.GetBytes("副う"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("厭うた"),
			to = eucjp.GetBytes("厭う"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("厭うて"),
			to = eucjp.GetBytes("厭う"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いた"),
			to = eucjp.GetBytes("く"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いだ"),
			to = eucjp.GetBytes("ぐ"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いて"),
			to = eucjp.GetBytes("く"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("いで"),
			to = eucjp.GetBytes("ぐ"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("えば"),
			to = eucjp.GetBytes("う"),
			type = 640,
			reason = DeinflectReason.Ba,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("える"),
			to = eucjp.GetBytes("う"),
			type = 513,
			reason = DeinflectReason.Potential,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("おう"),
			to = eucjp.GetBytes("う"),
			type = 640,
			reason = DeinflectReason.Volitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("仰い"),
			to = eucjp.GetBytes("仰る"),
			type = 640,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("仰い"),
			to = eucjp.GetBytes("仰る"),
			type = 640,
			reason = DeinflectReason.Imperative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("かず"),
			to = eucjp.GetBytes("く"),
			type = 640,
			reason = DeinflectReason.Zu,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("がず"),
			to = eucjp.GetBytes("ぐ"),
			type = 640,
			reason = DeinflectReason.Zu,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("かぬ"),
			to = eucjp.GetBytes("く"),
			type = 640,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("かん"),
			to = eucjp.GetBytes("く"),
			type = 640,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("がぬ"),
			to = eucjp.GetBytes("ぐ"),
			type = 640,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("がん"),
			to = eucjp.GetBytes("ぐ"),
			type = 640,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きた"),
			to = eucjp.GetBytes("くる"),
			type = 2176,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("きて"),
			to = eucjp.GetBytes("くる"),
			type = 2176,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("くて"),
			to = eucjp.GetBytes("い"),
			type = 1152,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("けば"),
			to = eucjp.GetBytes("く"),
			type = 640,
			reason = DeinflectReason.Ba,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("げば"),
			to = eucjp.GetBytes("ぐ"),
			type = 640,
			reason = DeinflectReason.Ba,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ける"),
			to = eucjp.GetBytes("く"),
			type = 513,
			reason = DeinflectReason.Potential,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("げる"),
			to = eucjp.GetBytes("ぐ"),
			type = 513,
			reason = DeinflectReason.Potential,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("こい"),
			to = eucjp.GetBytes("くる"),
			type = 2176,
			reason = DeinflectReason.Imperative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("こう"),
			to = eucjp.GetBytes("く"),
			type = 640,
			reason = DeinflectReason.Volitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ごう"),
			to = eucjp.GetBytes("ぐ"),
			type = 640,
			reason = DeinflectReason.Volitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("こず"),
			to = eucjp.GetBytes("くる"),
			type = 2176,
			reason = DeinflectReason.Zu,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("こぬ"),
			to = eucjp.GetBytes("くる"),
			type = 2176,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("こん"),
			to = eucjp.GetBytes("くる"),
			type = 2176,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("さず"),
			to = eucjp.GetBytes("す"),
			type = 640,
			reason = DeinflectReason.Zu,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("さぬ"),
			to = eucjp.GetBytes("す"),
			type = 640,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("さん"),
			to = eucjp.GetBytes("す"),
			type = 640,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("した"),
			to = eucjp.GetBytes("す"),
			type = 4736,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("した"),
			to = eucjp.GetBytes("する"),
			type = 4224,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("して"),
			to = eucjp.GetBytes("す"),
			type = 4736,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("して"),
			to = eucjp.GetBytes("する"),
			type = 4224,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しろ"),
			to = eucjp.GetBytes("す"),
			type = 4224,
			reason = DeinflectReason.Imperative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("しろ"),
			to = eucjp.GetBytes("する"),
			type = 4224,
			reason = DeinflectReason.Imperative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("せず"),
			to = eucjp.GetBytes("する"),
			type = 4224,
			reason = DeinflectReason.Zu,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("せぬ"),
			to = eucjp.GetBytes("する"),
			type = 4224,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("せん"),
			to = eucjp.GetBytes("する"),
			type = 4224,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("せず"),
			to = eucjp.GetBytes("す"),
			type = 4224,
			reason = DeinflectReason.Zu,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("せぬ"),
			to = eucjp.GetBytes("す"),
			type = 4224,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("せん"),
			to = eucjp.GetBytes("す"),
			type = 4224,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("せば"),
			to = eucjp.GetBytes("す"),
			type = 4736,
			reason = DeinflectReason.Ba,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("せよ"),
			to = eucjp.GetBytes("する"),
			type = 4224,
			reason = DeinflectReason.Imperative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("せよ"),
			to = eucjp.GetBytes("す"),
			type = 4224,
			reason = DeinflectReason.Imperative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("せる"),
			to = eucjp.GetBytes("す"),
			type = 513,
			reason = DeinflectReason.Potential,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("そう"),
			to = eucjp.GetBytes("い"),
			type = 1152,
			reason = DeinflectReason.Sou,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("そう"),
			to = eucjp.GetBytes("す"),
			type = 640,
			reason = DeinflectReason.Volitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("そう"),
			to = eucjp.GetBytes("る"),
			type = 2432,
			reason = DeinflectReason.Sou,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("たい"),
			to = eucjp.GetBytes("る"),
			type = 2308,
			reason = DeinflectReason.Tai,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("たず"),
			to = eucjp.GetBytes("つ"),
			type = 640,
			reason = DeinflectReason.Zu,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("たぬ"),
			to = eucjp.GetBytes("つ"),
			type = 640,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("たん"),
			to = eucjp.GetBytes("つ"),
			type = 640,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("たら"),
			to = eucjp.GetBytes("る"),
			type = 2432,
			reason = DeinflectReason.Tara,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("たり"),
			to = eucjp.GetBytes("る"),
			type = 2432,
			reason = DeinflectReason.Tari,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("った"),
			to = eucjp.GetBytes("う"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("った"),
			to = eucjp.GetBytes("つ"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("った"),
			to = eucjp.GetBytes("る"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("って"),
			to = eucjp.GetBytes("う"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("って"),
			to = eucjp.GetBytes("つ"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("って"),
			to = eucjp.GetBytes("る"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("てば"),
			to = eucjp.GetBytes("つ"),
			type = 640,
			reason = DeinflectReason.Ba,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("てる"),
			to = eucjp.GetBytes("つ"),
			type = 513,
			reason = DeinflectReason.Potential,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("てる"),
			to = eucjp.GetBytes("る"),
			type = 2305,
			reason = DeinflectReason.Continuous,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("とう"),
			to = eucjp.GetBytes("つ"),
			type = 640,
			reason = DeinflectReason.Volitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("とく"),
			to = eucjp.GetBytes("る"),
			type = 2306,
			reason = DeinflectReason.Toku,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ない"),
			to = eucjp.GetBytes("る"),
			type = 2308,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("なず"),
			to = eucjp.GetBytes("ぬ"),
			type = 640,
			reason = DeinflectReason.Zu,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("なぬ"),
			to = eucjp.GetBytes("ぬ"),
			type = 640,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("なん"),
			to = eucjp.GetBytes("ぬ"),
			type = 640,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ねば"),
			to = eucjp.GetBytes("ぬ"),
			type = 640,
			reason = DeinflectReason.Ba,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ねる"),
			to = eucjp.GetBytes("ぬ"),
			type = 513,
			reason = DeinflectReason.Potential,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("のう"),
			to = eucjp.GetBytes("ぬ"),
			type = 640,
			reason = DeinflectReason.Volitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ばず"),
			to = eucjp.GetBytes("ぶ"),
			type = 640,
			reason = DeinflectReason.Zu,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ばぬ"),
			to = eucjp.GetBytes("ぶ"),
			type = 640,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ばん"),
			to = eucjp.GetBytes("ぶ"),
			type = 640,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("べば"),
			to = eucjp.GetBytes("ぶ"),
			type = 640,
			reason = DeinflectReason.Ba,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("べる"),
			to = eucjp.GetBytes("ぶ"),
			type = 513,
			reason = DeinflectReason.Potential,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ぼう"),
			to = eucjp.GetBytes("ぶ"),
			type = 640,
			reason = DeinflectReason.Volitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ます"),
			to = eucjp.GetBytes("る"),
			type = 2432,
			reason = DeinflectReason.Polite,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("まず"),
			to = eucjp.GetBytes("む"),
			type = 640,
			reason = DeinflectReason.Zu,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("まぬ"),
			to = eucjp.GetBytes("む"),
			type = 640,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("まん"),
			to = eucjp.GetBytes("む"),
			type = 640,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("めば"),
			to = eucjp.GetBytes("む"),
			type = 640,
			reason = DeinflectReason.Ba,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("める"),
			to = eucjp.GetBytes("む"),
			type = 513,
			reason = DeinflectReason.Potential,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("もう"),
			to = eucjp.GetBytes("む"),
			type = 640,
			reason = DeinflectReason.Volitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("よう"),
			to = eucjp.GetBytes("る"),
			type = 2432,
			reason = DeinflectReason.Volitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("らず"),
			to = eucjp.GetBytes("る"),
			type = 640,
			reason = DeinflectReason.Zu,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("らぬ"),
			to = eucjp.GetBytes("る"),
			type = 640,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("らん"),
			to = eucjp.GetBytes("る"),
			type = 640,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("れば"),
			to = eucjp.GetBytes("る"),
			type = 7040,
			reason = DeinflectReason.Ba,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("れる"),
			to = eucjp.GetBytes("る"),
			type = 769,
			reason = DeinflectReason.Potential,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ろう"),
			to = eucjp.GetBytes("る"),
			type = 640,
			reason = DeinflectReason.Volitional,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("わず"),
			to = eucjp.GetBytes("う"),
			type = 640,
			reason = DeinflectReason.Zu,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("わぬ"),
			to = eucjp.GetBytes("う"),
			type = 640,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("わん"),
			to = eucjp.GetBytes("う"),
			type = 640,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んだ"),
			to = eucjp.GetBytes("ぬ"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んだ"),
			to = eucjp.GetBytes("ぶ"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んだ"),
			to = eucjp.GetBytes("む"),
			type = 640,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んで"),
			to = eucjp.GetBytes("ぬ"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んで"),
			to = eucjp.GetBytes("ぶ"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("んで"),
			to = eucjp.GetBytes("む"),
			type = 640,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("い"),
			to = eucjp.GetBytes("いる"),
			type = 384,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("い"),
			to = eucjp.GetBytes("う"),
			type = 640,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("い"),
			to = eucjp.GetBytes("る"),
			type = 2176,
			reason = DeinflectReason.Imperative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("え"),
			to = eucjp.GetBytes("う"),
			type = 640,
			reason = DeinflectReason.Imperative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("え"),
			to = eucjp.GetBytes("える"),
			type = 384,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("き"),
			to = eucjp.GetBytes("きる"),
			type = 384,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("き"),
			to = eucjp.GetBytes("く"),
			type = 640,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("き"),
			to = eucjp.GetBytes("くる"),
			type = 2176,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ぎ"),
			to = eucjp.GetBytes("ぎる"),
			type = 384,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ぎ"),
			to = eucjp.GetBytes("ぐ"),
			type = 640,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("き"),
			to = eucjp.GetBytes("い"),
			type = 1152,
			reason = DeinflectReason.Ki,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("く"),
			to = eucjp.GetBytes("い"),
			type = 1152,
			reason = DeinflectReason.Adv,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("け"),
			to = eucjp.GetBytes("く"),
			type = 640,
			reason = DeinflectReason.Imperative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("け"),
			to = eucjp.GetBytes("ける"),
			type = 384,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("げ"),
			to = eucjp.GetBytes("ぐ"),
			type = 640,
			reason = DeinflectReason.Imperative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("げ"),
			to = eucjp.GetBytes("げる"),
			type = 384,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("さ"),
			to = eucjp.GetBytes("い"),
			type = 1152,
			reason = DeinflectReason.Noun,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("し"),
			to = eucjp.GetBytes("す"),
			type = 640,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("じ"),
			to = eucjp.GetBytes("じる"),
			type = 384,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ず"),
			to = eucjp.GetBytes("る"),
			type = 2432,
			reason = DeinflectReason.Zu,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("せ"),
			to = eucjp.GetBytes("す"),
			type = 640,
			reason = DeinflectReason.Imperative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("せ"),
			to = eucjp.GetBytes("せる"),
			type = 384,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ぜ"),
			to = eucjp.GetBytes("ぜる"),
			type = 384,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("た"),
			to = eucjp.GetBytes("る"),
			type = 2432,
			reason = DeinflectReason.Past,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ち"),
			to = eucjp.GetBytes("ちる"),
			type = 384,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ち"),
			to = eucjp.GetBytes("つ"),
			type = 640,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("て"),
			to = eucjp.GetBytes("つ"),
			type = 640,
			reason = DeinflectReason.Imperative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("て"),
			to = eucjp.GetBytes("てる"),
			type = 384,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("て"),
			to = eucjp.GetBytes("る"),
			type = 2432,
			reason = DeinflectReason.Te,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("で"),
			to = eucjp.GetBytes("でる"),
			type = 384,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("な"),
			to = eucjp.GetBytes(""),
			type = 7040,
			reason = DeinflectReason.ImperativeNegative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("に"),
			to = eucjp.GetBytes("にる"),
			type = 384,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("に"),
			to = eucjp.GetBytes("ぬ"),
			type = 640,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ぬ"),
			to = eucjp.GetBytes("る"),
			type = 384,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ん"),
			to = eucjp.GetBytes("る"),
			type = 384,
			reason = DeinflectReason.Negative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ね"),
			to = eucjp.GetBytes("ぬ"),
			type = 640,
			reason = DeinflectReason.Imperative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ね"),
			to = eucjp.GetBytes("ねる"),
			type = 384,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ひ"),
			to = eucjp.GetBytes("ひる"),
			type = 384,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("び"),
			to = eucjp.GetBytes("びる"),
			type = 384,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("び"),
			to = eucjp.GetBytes("ぶ"),
			type = 640,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("へ"),
			to = eucjp.GetBytes("へる"),
			type = 384,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("べ"),
			to = eucjp.GetBytes("ぶ"),
			type = 640,
			reason = DeinflectReason.Imperative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("べ"),
			to = eucjp.GetBytes("べる"),
			type = 384,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("み"),
			to = eucjp.GetBytes("みる"),
			type = 384,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("み"),
			to = eucjp.GetBytes("む"),
			type = 640,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("め"),
			to = eucjp.GetBytes("む"),
			type = 640,
			reason = DeinflectReason.Imperative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("め"),
			to = eucjp.GetBytes("める"),
			type = 384,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("よ"),
			to = eucjp.GetBytes("る"),
			type = 384,
			reason = DeinflectReason.Imperative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("り"),
			to = eucjp.GetBytes("りる"),
			type = 384,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("り"),
			to = eucjp.GetBytes("る"),
			type = 640,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("れ"),
			to = eucjp.GetBytes("る"),
			type = 640,
			reason = DeinflectReason.Imperative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("れ"),
			to = eucjp.GetBytes("れる"),
			type = 384,
			reason = DeinflectReason.MasuStem,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("ろ"),
			to = eucjp.GetBytes("る"),
			type = 384,
			reason = DeinflectReason.Imperative,
		};
		list.Add(next);
		next = new DeinflectRule()
		{
			from = eucjp.GetBytes("する"),
			to = eucjp.GetBytes(""),
			type = 8208,
			reason = DeinflectReason.SuruNoun,
		};
		list.Add(next);

		deinflectRuleData = new DeinflectRule[list.Count];
		list.CopyTo(deinflectRuleData);
	}
}
