// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlInstructionBuilder.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Xml;
using System.Collections;

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for XmlInstructionBuilder.
	/// </summary>
	public class XmlInstructionBuilder
	{
		/// <summary>
		/// Build a dynamic instruction tree by parsing test script elements.
		/// The TestState must already be created.
		/// </summary>
		public XmlInstructionBuilder(){}

		/// <summary>
		/// Interpret a node as some kind of Instruction.
		/// Make the class but don't process its child instructions if any.
		/// Don't execute the instruction.
		/// Some instructions do not return objects:
		///   bug - a bug annotation element
		///	  #comment
		///	  #significant-whitespace
		///	  #whitespace
		/// The ones with '#' are generated by the XML parser.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be checked</param>
		/// <param name="con">The current context object</param>
		/// <returns>Returns an unexecuted instruction or null</returns>
		static public Instruction MakeShell(XmlNode xn, Context con)
		{
			// number the context if it doesn't have one yet to avoid depth-first numbering.
			if (con != null && con.Number == -1) con.Number = TestState.getOnly().IncInstructionCount;
			switch(xn.Name)
			{ // cases listed in ascending alphabetical order
				case "beep":
					return CreateBeep(xn, con);
				case "click":
					return CreateClick(xn, con);
				case "do-once":
					return CreateDoOnceContext(xn, con);
				case "file-comp":
					return CreateFileComp(xn, con);
				case "garbage":
					return CreateGarbage(xn, con);
				case "glimpse":
					return CreateGlimpse(xn, con);
				case "glimpse-extra":
					return CreateGlimpseExtra(xn, con);
				case "hover-over":
					return CreateHoverOver(xn, con);
				case "include":
					return CreateInclude(xn, con);
				case "insert":
					return CreateInsert(xn, con);
				case "if":
					return CreateIf(xn, con);
				case "match-strings":
					return CreateMatchStrings(xn, con);
				case "model":
					return CreateModelContext(xn, con);
				case "monitor-time":
					return CreateTime(xn, con);
				case "on-application":
					return CreateApplicationContext(xn, con);
				case "on-desktop":
					return CreateDesktopContext(xn, con);
				case "on-dialog":
					return CreateDialogContext(xn, con);
				case "on-startup":
					return CreateStartUpContext(xn, con);
				case "registry":
					return CreateRegistry(xn, con);
				case "skip":
					return new Skip();
				case "sound":
					return CreateSound(xn, con);
				case "select-text":
					return CreateSelectText(xn, con);
				case "var":
					return CreateVar(xn, con);
				case "bug": // bug annotation element
				case "#comment": // ignore comments, etc..
				case "#significant-whitespace":
				case "#whitespace":
					return null;
				default:
					Logger.getOnly().fail("Unexpected instruction <"+xn.Name+"> found");
					break;
			}
			return null;
		}

		/// <summary>
		/// Adds an instruction to the growing tree, sets the log level
		/// and records it to the log.
		/// </summary>
		/// <param name="xn">An element node</param>
		/// <param name="ins">The instruction to be added</param>
		/// <param name="con">The current context object</param>
		static private void AddInstruction(XmlNode xn, Instruction ins, Context con)
		{	// Vars put themselves on ts, making sure there is only one
			ins.Element = (XmlElement)xn;
			if (xn.Name != "var") CheckForId(xn, ins);
			// If not a new one, make sure the model node is propagated to each child context
			if (ins is Context && con.ModelNode != null)
				((Context)ins).ModelNode = con.ModelNode;
			ins.Parent = con;
			con.Add(ins);
			string logLevel = XmlFiler.getAttribute(xn, "log");
			if (logLevel != null && "all" == logLevel) ins.LogLevel = 1;
			if (logLevel != null && "time" == logLevel) ins.LogLevel = 2;
			// add one to the instruction count, then assign it to the instruction
			// A context might already have a number
			//if (ins.Number == -1) ins.Number = TestState.getOnly().IncInstructionCount;
			//Logger.getOnly().mark(ins); // log the progress of interpretation
		}

		/// <summary>
		/// Select the nodes from the GUI Model xPath select in the script context
		/// con.
		/// </summary>
		/// <param name="con">The script context who's model to use</param>
		/// <param name="select">Gui Model xPath to model nodes</param>
		/// <param name="source">Text to identify the source instruction in asserts</param>
		/// <returns>A list of model nodes matching the query</returns>
		static public XmlNodeList selectNodes(Context con, string select, string source)
		{
			XmlNodeList nodes = null;
			// try to dereference variables in the select expression
			string evalSelect = Utilities.evalExpr(select);
			if (con.ModelNode != null)
			{ // Search from the current model context
				XmlNode current = con.ModelNode.ModelNode;
				Logger.getOnly().isNotNull(current, source + " context model node is null, parent may not have @select");
				XmlNamespaceManager nsmgr = new XmlNamespaceManager(current.OwnerDocument.NameTable);
				//m_log.paragraph("Selecting from model context=" + current.Name);
				nodes = current.SelectNodes(evalSelect, nsmgr);
			}
			else
			{ // Search from the model root
				ApplicationContext apCon = null;
				if (typeof(ApplicationContext).IsInstanceOfType(con))
					apCon = (ApplicationContext)con;
				else
					apCon = (ApplicationContext)con.Ancestor(typeof(ApplicationContext));
				Logger.getOnly().isNotNull(apCon, "No on-application context found for " + source + "select to get a model from");
				XmlElement root = apCon.ModelRoot;
				Logger.getOnly().isNotNull(root, "GUI model of " + source + "select has no root");
				// preprocess select for special variables like $name; if there is a model node
				//m_log.paragraph("Selecting from model context=" + root.Name);
				nodes = root.SelectNodes(evalSelect);
			}
			return nodes;
		}

		/// <summary>
		/// Creates an application context instruction.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be checked</param>
		/// <param name="con">The current context object</param>
		static ApplicationContext CreateApplicationContext(XmlNode xn, Context con)
		{
			ApplicationContext ac = new ApplicationContext();
			ac.Run = XmlFiler.getAttribute(xn, "run");
			if (ac.Run == null || ac.Run == "") ac.Run = "ok";
			ac.Gui   = XmlFiler.getAttribute(xn, "gui");
			ac.Path  = XmlFiler.getAttribute(xn, "path");
			ac.Exe   = XmlFiler.getAttribute(xn, "exe");
			ac.Args  = XmlFiler.getAttribute(xn, "args");
			ac.Work  = XmlFiler.getAttribute(xn, "work");
			ac.Title = XmlFiler.getAttribute(xn, "title");
			string src = XmlFiler.getAttribute(xn, "source");
			if (src != null) ac.SetSource(src);
			ac.Close  = XmlFiler.getAttribute(xn, "close");
			ac.OnPass = XmlFiler.getAttribute(xn, "on-pass");
			ac.OnFail = XmlFiler.getAttribute(xn, "on-fail");
			string rest = XmlFiler.getAttribute(xn, "wait");
			if (rest != null) ac.Rest = Convert.ToInt32(rest);
			ac.Configure(); // set model root for use by children
			ac.ModelNode = con.ModelNode;
			AddInstruction(xn, ac, con);
			return ac;
		}

		static Model CreateModelContext(XmlNode xn, Context con)
		{ // check select and path attributes
			Model model = new Model();
			model.Select = XmlFiler.getAttribute(xn, "select");
			Logger.getOnly().isTrue(model.Select != null, "Model instruction must have a select attribute.");
			Logger.getOnly().isTrue(model.Select != "", "Model instruction must have a non-empty select.");
			model.When = XmlFiler.getAttribute(xn, "when");
			model.OnPass = XmlFiler.getAttribute(xn, "on-pass");
			model.OnFail = XmlFiler.getAttribute(xn, "on-fail");
			string rest = XmlFiler.getAttribute(xn, "wait");
			if (rest != null) model.Rest = Convert.ToInt32(rest);
			AddInstruction(xn, model, con);
			return model;
		}

		/// <summary>
		/// Creates a startup context instruction.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be checked</param>
		/// <param name="con">The current context object</param>
		static StartUpContext CreateStartUpContext(XmlNode xn, Context con)
		{
			StartUpContext su = new StartUpContext();
			su.OnPass = XmlFiler.getAttribute(xn, "on-pass");
			su.OnFail = XmlFiler.getAttribute(xn, "on-fail");
			su.ModelNode = con.ModelNode;
			AddInstruction(xn, su, con);
/*			foreach (XmlNode xnChild in xn.ChildNodes)
			{
				InterpretChild(xnChild, su);
			}
*/
			return su;
		}

		/// <summary>
		/// Creates a doOnce context instruction.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be checked</param>
		/// <param name="con">The current context object</param>
		static DoOnceContext CreateDoOnceContext(XmlNode xn, Context con)
		{
			string maxWait = XmlFiler.getAttribute(xn, "until");
//			Assert.IsTrue(maxWait == null || maxWait.Length == 0, "Do-Once context must have an until attribute.");

			Logger.getOnly().isNotNull(maxWait, "Do-Once context must have an until attribute.");
			Logger.getOnly().isTrue(maxWait != "", "Do-Once context must have a non-empty 'until' attribute.");

			DoOnceContext doOnce = new DoOnceContext(Convert.ToInt32(maxWait));
			doOnce.WaitingFor = XmlFiler.getAttribute(xn, "waiting-for");
			doOnce.OnPass = XmlFiler.getAttribute(xn, "on-pass");
			doOnce.OnFail = XmlFiler.getAttribute(xn, "on-fail");
			doOnce.ModelNode = con.ModelNode;
			AddInstruction(xn, doOnce, con);
/*			foreach (XmlNode xnChild in xn.ChildNodes)
			{
				InterpretChild(xnChild, doOnce);
			}
*/
			return doOnce;
		}

		/// <summary>
		/// Inserts the instructions in this include node.
		/// For now, a context is created to contain them.
		/// This will work until variables confrom to scoping rules -
		/// right now, all variables are global.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be checked</param>
		/// <param name="con">The current context object</param>
		static Include CreateInclude(XmlNode xn, Context con)
		{
			Include includeCon = new Include();
			string pathname = XmlFiler.getAttribute(xn, "from");
			Logger.getOnly().isNotNull(pathname, @"include must have a 'from' path.");
			Logger.getOnly().isTrue(pathname != "", @"include must have a 'from' path.");
			includeCon.From = pathname;
			AddInstruction(xn, includeCon, con);

			pathname = TestState.getOnly().getScriptPath() + @"\" + pathname;
			XmlDocument doc = new XmlDocument();
			doc.PreserveWhitespace = true; // allows <insert> </insert>
			try { doc.Load(pathname); }
			catch (System.IO.FileNotFoundException ioe)
			{
				Logger.getOnly().fail(@"include '" + pathname + "' not found: " + ioe.Message);
			}
			catch (System.Xml.XmlException xme)
			{
				Logger.getOnly().fail(@"include '" + pathname + "' not loadable: " + xme.Message);
			}
			XmlNode include = doc["include"];
			Logger.getOnly().isNotNull(include, "Missing document element 'include'.");
			XmlElement conEl = con.Element;
			// clone insert and add it before so there's an insert before and after
			// after adding elements, delete the "after" one
			XmlDocumentFragment df = xn.OwnerDocument.CreateDocumentFragment();
			df.InnerXml = xn.OuterXml;
			conEl.InsertBefore(df, xn);
			foreach (XmlNode xnode in include.ChildNodes)
			{
				string image = xnode.OuterXml;
				if (image.StartsWith("<"))
				{
					XmlDocumentFragment dfrag = xn.OwnerDocument.CreateDocumentFragment();
					dfrag.InnerXml = xnode.OuterXml;
					conEl.InsertBefore(dfrag,xn);
				}
			}
			conEl.RemoveChild(xn);
			//Logger.getOnly().paragraph(Utilities.attrText(textImage));
			return includeCon;
		}

		/// <summary>
		/// Creates a time context instruction.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be checked</param>
		/// <param name="con">The current context object</param>
		static TimeContext CreateTime(XmlNode xn, Context con)
		{
			string expect = XmlFiler.getAttribute(xn, "expect");
			TimeContext timeIt = new TimeContext(Convert.ToInt32(expect));
			string desc = XmlFiler.getAttribute(xn, "desc");
			timeIt.Decsription = desc;
/*			foreach (XmlNode xnChild in xn.ChildNodes)
			{
				InterpretChild(xnChild, timeIt);
			}
*/
			timeIt.ModelNode = con.ModelNode;
			AddInstruction(xn, timeIt, con);
			return timeIt;
		}

		/// <summary>
		/// Creates a desktop context instruction.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be created</param>
		/// <param name="con">The current context object</param>
		static Desktop CreateDesktopContext(XmlNode xn, Context con)
		{
			Desktop dt = new Desktop();
			string rest = XmlFiler.getAttribute(xn, "wait");
			if (rest != null) dt.Rest = Convert.ToInt32(rest);
			dt.OnPass = XmlFiler.getAttribute(xn, "on-pass");
			dt.OnFail = XmlFiler.getAttribute(xn, "on-fail");
			dt.ModelNode = con.ModelNode;
			AddInstruction(xn, dt, con);
/*			foreach (XmlNode xnChild in xn.ChildNodes)
			{
				InterpretChild(xnChild, dt);
			}
*/
			return dt;
		}

		/// <summary>
		/// Creates a dialog context instruction.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be created</param>
		/// <param name="con">The current context object</param>
		static DialogContext CreateDialogContext(XmlNode xn, Context con)
		{
			DialogContext dc = new DialogContext();
			dc.Name  = XmlFiler.getAttribute(xn, "name");
			dc.Title = XmlFiler.getAttribute(xn, "title");
			dc.Select = XmlFiler.getAttribute(xn, "select");
			Logger.getOnly().isTrue(dc.Title != null || dc.Select != null, "Dialog context '" + dc.Name + "' has no Title or selected model.");
			string rest = XmlFiler.getAttribute(xn, "wait");
			if (rest != null) dc.Rest = Convert.ToInt32(rest);
			string until = XmlFiler.getAttribute(xn, "until");
			if (until != null) dc.Until = Convert.ToInt32(until);
			dc.OnPass = XmlFiler.getAttribute(xn, "on-pass");
			dc.OnFail = XmlFiler.getAttribute(xn, "on-fail");
			dc.ModelNode = con.ModelNode;
			AddInstruction(xn, dc, con);
/*			foreach (XmlNode xnChild in xn.ChildNodes)
			{
				InterpretChild(xnChild, dc);
			}
*/
			return dc;
		}

		/// <summary>
		/// Creates a beep instruction.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be created</param>
		/// <param name="con">The current context object</param>
		static Beep CreateBeep(XmlNode xn, Context con)
		{
			Beep beep = CreateBeep(xn);
			AddInstruction(xn, beep, con);
			return beep;
		}

		/// <summary>
		/// Creates a beep instruction.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be created</param>
		/// <returns>A Beep intsruction</returns>
		static Beep CreateBeep(XmlNode xn)
		{
			Beep beep = new Beep();
			string rest = XmlFiler.getAttribute(xn, "wait");
			if (rest != null) beep.Rest = Convert.ToInt32(rest);
			return beep;
		}

		/// <summary>
		/// Creates a garbage instruction.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be created</param>
		/// <param name="con">The current context object</param>
		static Garbage CreateGarbage(XmlNode xn, Context con)
		{
			Garbage gc = CreateGarbage(xn);
			AddInstruction(xn, gc, con);
			return gc;
		}

		/// <summary>
		/// Creates a garbage instruction.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be created</param>
		/// <returns>A Garbage intsruction</returns>
		static Garbage CreateGarbage(XmlNode xn)
		{
			Garbage gc = new Garbage();
			string rest = XmlFiler.getAttribute(xn, "wait");
			if (rest != null) gc.Rest = Convert.ToInt32(rest);
			return gc;
		}

		/// <summary>
		/// Creates a click instruction.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be created</param>
		/// <param name="con">The current context object</param>
		static Click CreateClick(XmlNode xn, Context con)
		{ // check select and path attributes
			Click click = new Click();
			click.Select = XmlFiler.getAttribute(xn, "select");
			click.Path = XmlFiler.getAttribute(xn, "path");
			Logger.getOnly().isTrue(click.Path != null || click.Select != null, "Click instruction must have a path or select.");
			Logger.getOnly().isTrue(click.Path != "" || click.Select != "", "Click instruction must have a non-empty path or select.");
			click.When = XmlFiler.getAttribute(xn, "when");
			string until = XmlFiler.getAttribute(xn, "until");
			if (until != null) click.Until = Convert.ToInt32(until);
			string repeat = XmlFiler.getAttribute(xn, "repeat");
			if (repeat != null) click.Repeat = Convert.ToInt32(repeat);
			click.Side = XmlFiler.getAttribute(xn, "side");
			click.For = XmlFiler.getAttribute(xn, "for");
			string rest = XmlFiler.getAttribute(xn, "wait");
			if (rest != null)
			{
				if (rest.ToLower().Equals("no")) click.Rest = -1;
				else click.Rest = Convert.ToInt32(rest);
			}
			string dx = XmlFiler.getAttribute(xn, "dx");
			if (dx != null) click.Dx = Convert.ToInt32(dx);
			string dy = XmlFiler.getAttribute(xn, "dy");
			if (dy != null) click.Dy = Convert.ToInt32(dy);
			AddInstruction(xn, click, con);
			return click;
		}

		/// <summary>
		/// Creates a hover-over instruction.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be checked</param>
		/// <param name="con">The current context object</param>
		static HoverOver CreateHoverOver(XmlNode xn, Context con)
		{
			HoverOver hover = new HoverOver();
			hover.Path = XmlFiler.getAttribute(xn, "path");
			Logger.getOnly().isNotNull(hover.Path, "Hover-over instruction must have a path.");
			Logger.getOnly().isTrue(hover.Path != "", "Hover-over instruction must have a non-empty path.");
			string rest = XmlFiler.getAttribute(xn, "wait");
			if (rest != null) hover.Rest = Convert.ToInt32(rest);
			AddInstruction(xn, hover, con);
			return hover;
		}

		/// <summary>
		/// Creates a glimpse instruction and parses its content.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be checked</param>
		/// <param name="con">The current context object</param>
		static Glimpse CreateGlimpse(XmlNode xn, Context con)
		{
			Glimpse glimpse = new Glimpse();
			glimpse.Path   = XmlFiler.getAttribute(xn, "path");
			glimpse.Select = XmlFiler.getAttribute(xn, "select");
			Logger.getOnly().isTrue(glimpse.Path != null || glimpse.Select != null, "Glimpse instruction must have a path or select.");
			Logger.getOnly().isTrue(glimpse.Path != "" || glimpse.Select != "", "Glimpse instruction must have a non-empty path or select.");
			glimpse.Prop   = XmlFiler.getAttribute(xn, "prop");
			glimpse.Expect = XmlFiler.getAttribute(xn, "expect");
			glimpse.OnPass = XmlFiler.getAttribute(xn, "on-pass");
			glimpse.OnFail = XmlFiler.getAttribute(xn, "on-fail");
			string rest = XmlFiler.getAttribute(xn, "wait");
			if (rest != null) glimpse.Rest = Convert.ToInt32(rest);
			InterpretMessage(glimpse, xn.ChildNodes);
			AddInstruction(xn, glimpse, con);
			return glimpse;
		}

		/// <summary>
		/// Creates an insert instruction and parses its content.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be checked</param>
		/// <param name="con">The current context object</param>
		static Insert CreateInsert(XmlNode xn, Context con)
		{
			Insert insert = new Insert();
			string rest = XmlFiler.getAttribute(xn, "wait");
			if (rest != null) insert.Rest = Convert.ToInt32(rest);
			string pause = XmlFiler.getAttribute(xn, "pause");
			if (pause != null) insert.Pause = Convert.ToInt32(pause);
			foreach (XmlNode node in xn.ChildNodes)
			{
				switch (node.Name)
				{
					case "#text":
					case "#whitespace":
					case "#significant-whitespace": // a nameless text node
						insert.Text = node.Value;
						break;
					default:
						Logger.getOnly().fail("Insert instruction must have something to insert.");
						break;
				}
			}
			Logger.getOnly().isNotNull(insert.Text, "Insert instruction must have some content.");
			Logger.getOnly().isTrue(insert.Text != "", "Insert instruction must have non-empty content.");
			AddInstruction(xn, insert, con);
			return insert;
		}

		/// <summary>
		/// Creates a glimpse-extra instruction and parses its content.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be checked</param>
		/// <param name="con">The current context object</param>
		static GlimpseExtra CreateGlimpseExtra(XmlNode xn, Context con)
		{
			GlimpseExtra glimpse = new GlimpseExtra();
			glimpse.Path = XmlFiler.getAttribute(xn, "path");
			glimpse.SelectPath = XmlFiler.getAttribute(xn, "select-path");
			glimpse.Names = XmlFiler.getAttribute(xn, "names");
			glimpse.Select = XmlFiler.getAttribute(xn, "select");
			glimpse.OnPass = XmlFiler.getAttribute(xn, "on-pass");
			glimpse.OnFail = XmlFiler.getAttribute(xn, "on-fail");
			string rest = XmlFiler.getAttribute(xn, "wait");
			if (rest != null) glimpse.Rest = Convert.ToInt32(rest);
			InterpretMessage(glimpse,xn.ChildNodes);
			AddInstruction(xn, glimpse, con);
			return glimpse;
		}

		/// <summary>
		/// Creates an if instruction and parses its content.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be checked</param>
		/// <param name="ts">The TestState containing the named instruction list</param>
		/// <param name="con">The current context object</param>
		static If CreateIf(XmlNode xn, Context con)
		{
			Logger.getOnly().isNotNull(xn["condition"], "If instruction must have a condition.");
			If ifIns = new If();
			string rest = XmlFiler.getAttribute(xn, "wait");
			if (rest != null) ifIns.Rest = Convert.ToInt32(rest);
			AddInstruction(xn, ifIns, con);
			foreach (XmlNode elt in xn.ChildNodes)
			{
				switch (elt.Name)
				{
					case "condition":
						Condition cond = CreateCondition(elt);
						ifIns.AddCondition(cond);
						break;
				}
			}
			return ifIns;
		}

		/// <summary>
		/// Creates and parses a condition.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be checked</param>
		/// <returns>A Condition intsruction</returns>
		static Condition CreateCondition(XmlNode xn)
		{
			Condition cond = new Condition();
			cond.Of = XmlFiler.getAttribute(xn, "of"); // can be an id, 'literal' or number
			Logger.getOnly().isNotNull(cond.Of, "Condition must have an 'of' attribute.");
			Logger.getOnly().isTrue(cond.Of != "", "Condition must have a non-null 'of' attribute.");
			cond.Is = XmlFiler.getAttribute(xn, "is"); // can be equal, true, false, 'literal' or number
			Logger.getOnly().isNotNull(cond.Is, "Condition must have an 'is' attribute.");
			Logger.getOnly().isTrue(cond.Is != "", "Condition must have a non-null 'is' attribute.");
			cond.To = XmlFiler.getAttribute(xn, "to"); // can be an id, 'literal' or number

			foreach (XmlNode condElt in xn.ChildNodes)
			{
				switch (condElt.Name)
				{
					case "condition":
						Condition condChild = CreateCondition(condElt);
						cond.AddCondition(condChild);
						break;
				}
			}
			return cond;
		}

		/// <summary>
		/// Creates and parses a message contained in some instructions.
		/// </summary>
		/// <param name="ins"></param>
		/// <param name="body"></param>
		static void InterpretMessage(CheckBase ins, XmlNodeList body)
		{
			if (body != null )
			{
				Message message = new Message();

				foreach (XmlNode node in body)
				{
					switch (node.Name)
					{
						case "#text": // a nameless text node
							message.AddText(node.Value);
							break;
						case "data":
							message.AddDataRef(XmlFiler.getAttribute(node,"of"),ins);
							break;
						case "beep":
							message.AddSound(CreateBeep(node));
							break;
						case "sound":
							message.AddSound(CreateSound(node));
							break;
					}
				}
				ins.Message = message;
			}
		}

		/// <summary>
		/// Creates a select-text instruction.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be checked</param>
		/// <param name="con">The current context object</param>
		static SelectText CreateSelectText(XmlNode xn, Context con)
		{
			SelectText sel = new SelectText();
			sel.Path = XmlFiler.getAttribute(xn, "path");
			Logger.getOnly().isNotNull(sel.Path, "Select-text instruction must have a path.");
			Logger.getOnly().isTrue(sel.Path != "", "Select-text instruction must have a non-empty path.");
			sel.Loc = XmlFiler.getAttribute(xn, "loc");
			string sAt = XmlFiler.getAttribute(xn, "at");
			if (sAt == null) sel.At = 0;
			else             sel.At = Convert.ToInt32(sAt);
			string sRun = XmlFiler.getAttribute(xn, "run");
			if (sRun == null) sel.Run = 0;
			else              sel.Run = Convert.ToInt32(sRun);
			string rest = XmlFiler.getAttribute(xn, "wait");
			if (rest != null) sel.Rest = Convert.ToInt32(rest);
			AddInstruction(xn, sel, con);
			return sel;
		}

		/// <summary>
		/// Creates a registry instruction.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be checked</param>
		/// <param name="con">The current context object</param>
		static Registry CreateRegistry(XmlNode xn, Context con)
		{
			string key = XmlFiler.getAttribute(xn, "key");
			string data = XmlFiler.getAttribute(xn, "data");
			Logger.getOnly().isTrue(key != null && data != null, "Registry instruction must have a key and data.");
			Registry reg = new Registry(key, data);
			AddInstruction(xn, reg, con);
			return reg;
		}

		/// <summary>
		/// Creates a sound instruction.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be checked</param>
		/// <param name="con">The current context object</param>
		static Sound CreateSound(XmlNode xn, Context con)
		{
			Sound sound = CreateSound(xn);
			AddInstruction(xn, sound, con);
			return sound;
		}

		/// <summary>
		/// Creates a sound instruction.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be checked</param>
		/// <returns>A Sound intsruction</returns>
		static Sound CreateSound(XmlNode xn)
		{
			string frequency = XmlFiler.getAttribute(xn, "frequency");
			string duration = XmlFiler.getAttribute(xn, "duration");
			Logger.getOnly().isTrue(frequency != null && duration != null, "Sound instruction must have a frequency and duration.");
			Sound sound = new Sound(Convert.ToUInt32(frequency), Convert.ToUInt32(duration));
			string rest = XmlFiler.getAttribute(xn, "wait");
			if (rest != null) sound.Rest = Convert.ToInt32(rest);
			return sound;
		}

		/// <summary>
		/// Creates a file-comp instruction and parses its content.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be checked</param>
		/// <param name="con">The current context object</param>
		static FileComp CreateFileComp(XmlNode xn, Context con)
		{
			FileComp fc = new FileComp();
			fc.Of = XmlFiler.getAttribute(xn, "of");
			Logger.getOnly().isNotNull(fc.Of, "File-Comp instruction must have an 'of'.");
			Logger.getOnly().isTrue(fc.Of != "", "File-Comp instruction must have a non-empty 'of'.");
			fc.To  = XmlFiler.getAttribute(xn, "to");
			Logger.getOnly().isNotNull(fc.To, "File-Comp instruction must have a 'to'.");
			Logger.getOnly().isTrue(fc.To != "", "File-Comp instruction must have a non-empty 'to'.");
			fc.OnPass = XmlFiler.getAttribute(xn, "on-pass");
			fc.OnFail = XmlFiler.getAttribute(xn, "on-fail");
			string rest = XmlFiler.getAttribute(xn, "wait");
			if (rest != null) fc.Rest = Convert.ToInt32(rest);
			InterpretMessage(fc,xn.ChildNodes);
			AddInstruction(xn, fc, con);
			return fc;
		}

		/// <summary>
		/// Creates a match-strings instruction and parses its content.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be checked</param>
		/// <param name="con">The current context object</param>
		static MatchStrings CreateMatchStrings(XmlNode xn, Context con)
		{
			MatchStrings ms = new MatchStrings();
			ms.Of = XmlFiler.getAttribute(xn, "of");
			Logger.getOnly().isNotNull(ms.Of, "Match-strings instruction must have an 'of'.");
			Logger.getOnly().isTrue(ms.Of != "", "Match-strings instruction must have a non-empty 'of'.");
			ms.To  = XmlFiler.getAttribute(xn, "to");
			Logger.getOnly().isNotNull(ms.To, "Match-strings instruction must have a 'to'.");
			Logger.getOnly().isTrue(ms.To != "", "Match-strings instruction must have a non-empty 'to'.");
			ms.Expect = XmlFiler.getAttribute(xn, "expect");
			ms.OnPass = XmlFiler.getAttribute(xn, "on-pass");
			ms.OnFail = XmlFiler.getAttribute(xn, "on-fail");
			string rest = XmlFiler.getAttribute(xn, "wait");
			if (rest != null) ms.Rest = Convert.ToInt32(rest);
			InterpretMessage(ms,xn.ChildNodes);
			AddInstruction(xn, ms, con);
			return ms;
		}

		/// <summary>
		/// Creates a variable to be referenced by other instruction attributes.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be checked</param>
		/// <param name="con">The current context object</param>
		public static Var CreateVar(XmlNode xn, Context con)
		{
			Var var  = new Var();
			var.Id   = XmlFiler.getAttribute(xn, "id");
			Logger.getOnly().isNotNull(var.Id, "Var instruction must have a id.");
			Logger.getOnly().isTrue(var.Id != "", "Var instruction must have a non-empty id.");
			string s = XmlFiler.getAttribute(xn, "set");
			// select should be move to var.execute so it is dynamic!
			string select = XmlFiler.getAttribute(xn, "select");
			if (select != null && select != "")
			{ // the variable can only have one text node or one other node assigned to it.
				XmlNodeList pathNodes = selectNodes(con, select, "var"+var.Id);
				Logger.getOnly().isNotNull(pathNodes, "var " + var.Id + " select='" + select + "' returned no result");
				if (pathNodes.Count > 0)
				{ // append first node to set string
					XmlNode modNode = pathNodes.Item(0);
					// which property of the node to get?
					string prop = null;
					string propName = XmlFiler.getAttribute(xn, "prop");
					if (propName == null && modNode is XmlElement) propName = "path";
					if (propName == null) propName = "value";
					if (propName != null && propName == "value") prop = XmlPath.ResolveModelPath(modNode, modNode.Value);
					if (propName != null && propName == "name") prop = modNode.Name;
					if (propName != null && propName == "type") prop = modNode.NodeType.ToString();
					if (propName != null && propName == "path")
					{
						XmlPath xp = new XmlPath(modNode);
						if (xp.isValid()) prop = xp.Path;
						else prop = null;
					}
					s += prop;
				}
				else s += "#NoSelection#";
			}
			var.Set = s;
			string when = XmlFiler.getAttribute(xn, "when");
			string add = XmlFiler.getAttribute(xn, "add");
			if (add != null) var.Add = add;
			if (var.Set == null && when == null)
			{ // if there is a select/when then don't complain if the select found nothing
				Logger.getOnly().isNotNull(var.Add, "Var " + var.Id +
					@" set, select or add must result in a string or number value unless when=""exists"" is set.");
				if (select != null && select != "") var.Set = @"#not-"+when+@"#";
			}
			string exists = XmlFiler.getAttribute(xn, "file-exists");
			if (exists != null) var.FileExists = exists;
			string rest = XmlFiler.getAttribute(xn, "wait");
			if (rest != null) var.Rest = Convert.ToInt32(rest);
			AddInstruction(xn, var, con);
			return var;
		}

		/// <summary>
		/// Determines if an instruction has an id. If it does, it adds to the named instruction list.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be checked</param>
		/// <param name="ins">The instruction to be checked</param>
		static void CheckForId(XmlNode xn, Instruction ins)
		{
			XmlAttribute xa = xn.Attributes["id"];
			if (xa != null)
			{
				TestState.getOnly().AddNamedInstruction(xa.Value, ins);
				ins.Id = xa.Value;
			}
		}
	}
}