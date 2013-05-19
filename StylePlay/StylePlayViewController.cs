using System;
using System.Drawing;

using System.Linq;
using MonoTouch.UIKit;
using System.Collections.Generic;

namespace StylePlay
{
	public static class StyleFinderHelper
	{
		public static StyleClassList CurrentList { get; set;}
		public static IEnumerable<string> StyleClassListFor(UIView target)
		{
			if (CurrentList == null)
				return null;

			return CurrentList.ClassesFor(target);
			/*
			var view = target as UIView;
			while (view != null)
			{
				var candidate = view as IStyleListContainer;
				if (candidate != null)
					return candidate.StyleClassList.ClassesFor(view);
				
				view = view.Superview;
			}
			
			return new string[0];
			*/
		}
	}

	public class StyleRuleSet
	{
		public List<Rule> Rules { get; private set; }

		public StyleRuleSet ()
		{
			Rules = new List<Rule>();
		}

		public IEnumerable<IStyle> StylesFor(object target)
		{
			var matchingRules = 
				from rule in Rules
					where rule.Matches(target)
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

		public bool Matches(object target)
		{
			return Selectors.Any(x => x.Matches(target));
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
		bool Matches(object target);
	}

	// need to have parent -> child relationships here... maybe more imporant than doing list...
	public class Selector : ISelector
	{
		public bool Matches(object target)
		{
			return
				TypeMatches(target)
					&& ClassMatches(target)
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

		private bool ClassMatches(object target)
		{
			if (string.IsNullOrEmpty(ClassName))
				return true;

			var styleClasses = StyleFinderHelper.StyleClassListFor((UIView)target);

			if (styleClasses == null)
				return false;

			return styleClasses.Contains(ClassName);
		}

		private bool ParentSelectorMatches(object target)
		{
			if (ParentSelector == null)
				return true;

			// TODO - trawl through the superviews...
			var uiView = target as UIView;
			if (uiView == null)
				return false;

			uiView = uiView.Superview;
			while (uiView != null)
			{
				if (ParentSelector.Matches(uiView))
				{
					return true;
				}

			    uiView = uiView.Superview;
            }

			// 			var styles = StyleFinderHelper.StyleClassListFor((UIView)target);
			//uiView.Superview;
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
			_appliers[new Key(typeof(UIView), "BorderColor")] = (item, value) =>
			{
				((UIView)item).Layer.BorderColor = ((UIColor)value).CGColor;
			};
			_appliers[new Key(typeof(UIView), "BorderWidth")] = (item, value) =>
			{
				((UIView)item).Layer.BorderWidth = (float)value;
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
			_appliers[new Key(typeof(UILabel), "Font")] = (item, value) =>
			{
				var fontDesc = (FontDescription)value;
				((UILabel)item).Font = UIFont.FromName(fontDesc.FontName, fontDesc.FontSize);
			};
			_appliers[new Key(typeof(UIButton), "Font")] = (item, value) =>
			{
				var fontDesc = (FontDescription)value;
				((UIButton)item).Font = UIFont.FromName(fontDesc.FontName, fontDesc.FontSize);
			};
			_appliers[new Key(typeof(UITextField), "Font")] = (item, value) =>
			{
				var fontDesc = (FontDescription)value;
				((UITextField)item).Font = UIFont.FromName(fontDesc.FontName, fontDesc.FontSize);
			};
		}
	}

	public class FontDescription
	{
		public string FontName {get;set;}
		public float FontSize {get;set;}
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

	public interface IStyleListContainer
	{
		StyleClassList StyleClassList { get; }
	}

	public partial class StylePlayViewController 
		: UIViewController
		, IStyleListContainer
	{
		public StylePlayViewController () : base ("StylePlayViewController", null)
		{
		}

		public StyleClassList StyleClassList
		{
			get; set;
		}

		void Apply (StyleRuleSet ruleset, StyleApplier applier, UIView parentView)
		{
			var styles = ruleset.StylesFor (parentView);
			foreach (var style in styles) {
				applier.Apply (parentView, style);
			}

			foreach (var view in parentView.Subviews) {
				Apply (ruleset, applier, view);
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
			rule.Styles.Add(new SimpleStyle("ShadowOpacity", 0.9f));
			rule.Styles.Add(new SimpleStyle("BorderColor", UIColor.Magenta));
			rule.Styles.Add(new SimpleStyle("BorderWidth", 3.0f));
			rule.Styles.Add(new SimpleStyle("Font", new FontDescription()
			                                {
				FontName = "Chalkduster",
				FontSize = 24f
			}));

			//
			ruleset.Rules.Add(rule);

			rule = new Rule();
			rule.Selectors.Add(new Selector(typeof(UILabel), "Foo"));
			rule.Styles.Add(new SimpleStyle("Color", UIColor.Magenta));
			rule.Styles.Add(new SimpleStyle("CornerRadius", 0f));
			rule.Styles.Add(new SimpleStyle("Font", new FontDescription()
			                                {
				FontName = "Baskerville-BoldItalic",
				FontSize = 24f
			}));
			ruleset.Rules.Add(rule);


			rule = new Rule();
			var parentSelector = new Selector(typeof(UIButton));
			rule.Selectors.Add(new Selector(typeof(UILabel), null, parentSelector));
			rule.Styles.Add(new SimpleStyle("BackgroundColor", UIColor.Black));
			ruleset.Rules.Add(rule);

			// would be nice to find another way to store these class hooks
			// could just about use Tag fields - but that's unpleasant :/
			StyleClassList = new StyleClassList();
			StyleClassList.AddClass(SpecialLabel, "Foo");

			StyleFinderHelper.CurrentList = StyleClassList;
			var applier = new StyleApplier();
			Apply (ruleset, applier, View);
		}
		
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			// Return true for supported orientations
			return (toInterfaceOrientation != UIInterfaceOrientation.PortraitUpsideDown);
		}
	}
}

