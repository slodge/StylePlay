using System;
using System.Drawing;

using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Collections.Generic;

namespace StylePlay
{
	public class StyleRuleSet
	{
		public List<Rule> Rules { get; private set; }

		public StyleRuleSet ()
		{
			Rules = new List<Rule>();
		}

		public IEnumerable<IStyle> StylesFor(object target, StyleClassList styleClassList)
		{
			var styles = styleClassList.ClassesFor(target);

			var matchingRules = 
				from rule in Rules
					where rule.Matches(target, styles)
					select rule;

			var toApply = new Dictionary<string, IStyle>();
			foreach (var rule in matchingRules)
			{
				foreach (var style in rule.Styles)
					toApply[style.Name] = style;
			}

			return toApply.Values;
		}
	}

	public class Rule
	{
		public List<ISelector> Selectors { get; private set; }
		public List<IStyle> Styles { get; private set; }

		public bool Matches(object target, IEnumerable<string> styleClasses)
		{
			return Selectors.Any(x => x.Matches(target, styleClasses));
		}

		public Rule ()
		{
			Selectors = new List<ISelector>();
			Styles = new List<IStyle>();
		}
	}

	// would be nice to override things...

	public interface ISelector
	{
		bool Matches(object target, IEnumerable<string> styleClasses);
	}

	// need to have parent -> child relationships here... maybe more imporant than doing list...
	public class Selector : ISelector
	{
		public bool Matches(object target, IEnumerable<string> styleClasses)
		{
			return
				TypeMatches(target)
					&& ClassMatches(styleClasses)
					&& ParentSelectorMatches(target);
		}

		private bool TypeMatches(object target)
		{
			if (TargetType == null)
				return true;

			if (TargetType.IsInstanceOfType(target))
				return true;

			return false;
		}

		private bool ClassMatches(IEnumerable<string> styleClasses)
		{
			if (string.IsNullOrEmpty(ClassName))
				return true;

			if (styleClasses == null)
				return false;

			return styleClasses.Contains(ClassName);
		}

		private bool ParentSelectorMatches(object target)
		{
			if (ParentSelector == null)
				return true;

			// TODO - trawl through the superviews...
			return false;
		}

		public Type TargetType { get; private set; }
		public string ClassName { get; private set; }
		public ISelector ParentSelector { get; private set; }

		public Selector (Type targetType = null, string className = null, ISelector parentSelector = null)
		{
			TargetType = targetType;
			ClassName = className;
			ParentSelector = parentSelector;
		}
	}

	public interface IStyle
	{
		string Name { get; }
		object Data { get; }
	}

	public class SimpleStyle : IStyle
	{
		public string Name { get; private set; }
		public object Data { get; private set; }

		public SimpleStyle (string name, object data)
		{
			Name = name;
			Data = data;
		}
	}

	public interface IStyleApplier
	{
		void Apply(object target, IStyle style);
	}

	public class StyleApplier
	{
		public void Apply(object target, IStyle style)
		{
			// need to find the way to get the code working...
			// Mvx binding already has it... :)
			// but for now let's hack...
			var applier = FindApplier(target, style.Name);
			if (applier != null)
				applier(target, style.Data);
		}

		private Action<object, object> FindApplier(object target, string name)
		{
			var type = target.GetType();
			return FindApplier(type, name);
		}

		private Action<object, object> FindApplier(Type type, string name)
		{
			var key = new Key(type, name);
			Action<object, object> action;
			if (_appliers.TryGetValue(key, out action))
				return action;

			if (type == typeof(Object))
				return null;

			if (type.BaseType == null)
				return null;

			return FindApplier(type.BaseType, name);
		}

		public class Key
		{
			public Type Type { get; private set; }
			public string Name { get; private set; }

			public Key (Type type, string name)
			{
				Type = type;
				Name = name;
			}

			public override bool Equals (object obj)
			{
				var rhs = obj as Key;
				if (rhs == null)
					return false;

				return (rhs.Type == Type &&
				        rhs.Name == Name);
			}

			public override int GetHashCode ()
			{
				return Type.GetHashCode () + Name.GetHashCode();
			}
		}

		private Dictionary<Key, Action<object, object>> _appliers;

		public StyleApplier ()
		{
			_appliers = new Dictionary<Key, Action<object, object>>();
			_appliers[new Key(typeof(UILabel), "Color")] = (item, value) =>
			{
				((UILabel)item).TextColor = (UIColor)value;
			};
			_appliers[new Key(typeof(UIView), "CornerRadius")] = (item, value) =>
			{
				((UIView)item).Layer.CornerRadius = (float)value;
			};
			_appliers[new Key(typeof(UIView), "BackgroundColor")] = (item, value) =>
			{
				((UIView)item).BackgroundColor = (UIColor)value;
			};
			_appliers[new Key(typeof(UIView), "ShadowColor")] = (item, value) =>
			{
				((UIView)item).Layer.ShadowColor = ((UIColor)value).CGColor;
				((UIView)item).Layer.MasksToBounds = false; // would be nice to only set this once...
			};
			_appliers[new Key(typeof(UIView), "ShadowRadius")] = (item, value) =>
			{
				((UIView)item).Layer.ShadowRadius = (float)value;
				((UIView)item).Layer.MasksToBounds = false; // would be nice to only set this once...
			};
			_appliers[new Key(typeof(UIView), "ShadowOffset")] = (item, value) =>
			{
				((UIView)item).Layer.ShadowOffset = (SizeF)value;
				((UIView)item).Layer.MasksToBounds = false; // would be nice to only set this once...
			};
			_appliers[new Key(typeof(UIView), "ShadowOpacity")] = (item, value) =>
			{
				((UIView)item).Layer.ShadowOpacity = (float)value;
				((UIView)item).Layer.MasksToBounds = false; // would be nice to only set this once...
			};
			_appliers[new Key(typeof(UIButton), "Color")] = (item, value) =>
			{
				// TODO - other UIControlState's
				((UIButton)item).SetTitleColor((UIColor)value, UIControlState.Normal);
			};
		}
	}

	// consider selected rules in a different way - don't try changing the contents when selected!

	/*
	// TODO - sub state stuff
	public class StyleRule
	{
		public UIControlState SubState { get; private set; }
		public object Value { get; private set; }
	}
	*/
	
	public class StyleClassList
	{
		public Dictionary<object, List<string>> Classes { get; private set; }

		public StyleClassList ()
		{
			Classes = new Dictionary<object, List<string>>();
		}

		public void AddClass(object target, string className)
		{
			List<string> classList;
			if (!Classes.TryGetValue(target, out classList))
			{
				classList = new List<string>();
				Classes[target] = classList;
			}

			classList.Add(className);
		}

		public IEnumerable<string> ClassesFor(object target)
		{
			List<string> classList;
			if (!Classes.TryGetValue(target, out classList))
			{
				return null;
			}

			return classList;
		}
	}

	public partial class StylePlayViewController : UIViewController
	{
		public StylePlayViewController () : base ("StylePlayViewController", null)
		{
		}

		void Apply (StyleRuleSet ruleset, StyleApplier applier, StyleClassList styleClassList, UIView parentView)
		{
			var styles = ruleset.StylesFor (parentView, styleClassList);
			foreach (var style in styles) {
				applier.Apply (parentView, style);
			}

			foreach (var view in parentView.Subviews) {
				Apply (ruleset, applier, styleClassList, view);
			}
		}
		
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			// obviously the ruleset should be from a file somehow...
			var ruleset = new StyleRuleSet();

			var rule = new Rule();
			rule.Selectors.Add(new Selector(typeof(UILabel)));
			rule.Styles.Add(new SimpleStyle("Color", UIColor.Green));
			rule.Styles.Add(new SimpleStyle("CornerRadius", 10f));
			rule.Styles.Add(new SimpleStyle("BackgroundColor", UIColor.Blue));
			ruleset.Rules.Add(rule);

			rule = new Rule();
			rule.Selectors.Add(new Selector(typeof(UIButton)));
			rule.Styles.Add(new SimpleStyle("Color", UIColor.Cyan));
			rule.Styles.Add(new SimpleStyle("CornerRadius", 30f));
			rule.Styles.Add(new SimpleStyle("BackgroundColor", UIColor.Orange));
			rule.Styles.Add(new SimpleStyle("ShadowOffset", new SizeF(4, 10)));
			rule.Styles.Add(new SimpleStyle("ShadowRadius", 7f));
			rule.Styles.Add(new SimpleStyle("ShadowColor", UIColor.Black));
			rule.Styles.Add(new SimpleStyle("ShadowOpacity", 0.5f));
			//
			ruleset.Rules.Add(rule);

			rule = new Rule();
			rule.Selectors.Add(new Selector(typeof(UILabel), "Foo"));
			rule.Styles.Add(new SimpleStyle("Color", UIColor.Magenta));
			rule.Styles.Add(new SimpleStyle("CornerRadius", 0f));
			ruleset.Rules.Add(rule);

			// would be nice to find another way to store these class hooks
			// could just about use Tag fields - but that's unpleasant :/
			var styleClassList = new StyleClassList();
			styleClassList.AddClass(SpecialLabel, "Foo");

			var applier = new StyleApplier();
			Apply (ruleset, applier, styleClassList, View);
		}
		
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			// Return true for supported orientations
			return (toInterfaceOrientation != UIInterfaceOrientation.PortraitUpsideDown);
		}
	}
}

