//--------------------------------------------------------------------------------------------------
// <copyright file="WixLocalizedCategoryAttribute.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Contains the WixLocalizedCategoryAttribute class.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
	using System;
	using Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure;

	/// <summary>
	/// Subclasses <see cref="CategoryAttribute"/> to allow for localized strings retrieved
	/// from the resource assembly.
	/// </summary>
	[AttributeUsage(AttributeTargets.All)]
	public class WixLocalizedCategoryAttribute : LocalizedCategoryAttribute
	{
		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="WixLocalizedCategoryAttribute"/> class.
		/// </summary>
		/// <param name="id">The string identifier to get.</param>
		public WixLocalizedCategoryAttribute(WixStrings.StringId id)
			: base(id.ToString())
		{
		}
		#endregion
	}
}