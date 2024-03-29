// pngwio.cs - functions for data output
//
// Based on libpng version 1.4.3 - June 26, 2010
// This code is released under the libpng license.
// For conditions of distribution and use, see copyright notice in License.txt
// Copyright (C) 2007-2010 by the Authors
// Copyright (c) 1998-2010 Glenn Randers-Pehrson
// (Version 0.96 Copyright (c) 1996, 1997 Andreas Dilger)
// (Version 0.88 Copyright (c) 1995, 1996 Guy Eric Schalnat, Group 42, Inc.)
//
// This file provides a location for all output. Users who need
// special handling are expected to write functions that have the same
// arguments as these and perform similar functions, but that possibly
// use different output methods.

using System;
using System.Collections.Generic;
using System.Text;

namespace Free.Ports.libpng
{
	public partial class png_struct
	{
		// This is the function that does the actual writing of data.
		void png_write_data(byte[] data, uint start, uint length)
		{
			if(start>PNG.UINT_31_MAX||length>PNG.UINT_31_MAX) throw new PNG_Exception("Index out of bounds");
			try
			{
				io_ptr.Write(data, (int)start, (int)length);
			}
			catch(Exception ex)
			{
				throw new PNG_Exception("Write Error", ex);
			}
		}
	}
}
