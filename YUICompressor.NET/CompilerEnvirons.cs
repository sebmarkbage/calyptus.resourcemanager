/* -*- Mode: java; tab-width: 8; indent-tabs-mode: nil; c-basic-offset: 4 -*-
 *
 * ***** BEGIN LICENSE BLOCK *****
 * Version: MPL 1.1/GPL 2.0
 *
 * The contents of this file are subject to the Mozilla Public License Version
 * 1.1 (the "License"); you may not use this file except in compliance with
 * the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 *
 * Software distributed under the License is distributed on an "AS IS" basis,
 * WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
 * for the specific language governing rights and limitations under the
 * License.
 *
 * The Original Code is Rhino code, released
 * May 6, 1999.
 *
 * The Initial Developer of the Original Code is
 * Netscape Communications Corporation.
 * Portions created by the Initial Developer are Copyright (C) 1997-1999
 * the Initial Developer. All Rights Reserved.
 *
 * Contributor(s):
 *   Igor Bukanov, igor@fastmail.fm
 *   Bob Jervis
 *
 * Alternatively, the contents of this file may be used under the terms of
 * the GNU General Public License Version 2 or later (the "GPL"), in which
 * case the provisions of the GPL are applicable instead of those above. If
 * you wish to allow use of your version of this file only under the terms of
 * the GPL and not to allow others to use your version of this file under the
 * MPL, indicate your decision by deleting the provisions above and replacing
 * them with the notice and other provisions required by the GPL. If you do
 * not delete the provisions above, a recipient may use your version of this
 * file under either the MPL or the GPL.
 *
 * ***** END LICENSE BLOCK ***** */
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Mozilla.JavaScript
{		
	public class CompilerEnvirons
	{
		public int LanguageVersion { get; set; }
		public bool GenerateDebugInfo { get; set; }
		public bool UseDynamicScope { get; set; }
		public bool ReservedKeywordAsIdentifier { get; set; }
		public bool AllowMemberExprAsFunctionName { get; set; }
		public bool XmlAvialable { get; set; }
		public int OptimizationLevel { get; set; }
		public bool GeneratingSource { get; set; }
		public bool StrictMode { get; set; }
		public bool WarningAsError { get; set; }
		public bool GenerateObserverCount { get; set; }
		public Dictionary<int, string> ActivationNames { get; set; }

		public CompilerEnvirons()
		{
			LanguageVersion = 1;
			GenerateDebugInfo = true;
			UseDynamicScope = false;
			ReservedKeywordAsIdentifier = false;
			AllowMemberExprAsFunctionName = false;
			XmlAvailable = true;
			OptimizationLevel = 0;
			GeneratingSource = true;
			StrictMode = false;
			WarningAsError = false;
			GenerateObserverCount = false;
		}
	} 
}