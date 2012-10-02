/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: UtilXml.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	Utility functions used in XML import or export which may be useful elsewhere as well.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef UTILXML_H_INCLUDED
#define UTILXML_H_INCLUDED

void WriteXmlUnicode(IStream * pstrm, const OLECHAR * rgchTxt, int cchTxt);

int CountXmlUtf8FromUtf16(const wchar * rgchwSrc, int cchwSrc, bool fXml = true);
int ConvertUtf16ToXmlUtf8(char * rgchDst, int cchMaxDst, const wchar * rgchwSrc, int cchwSrc,
	bool fXml = true);
int CountUtf16FromUtf8(const char * pszUtf8);
int CountUtf16FromUtf8(const char * rgchUtf8, int cch);
int SetUtf16FromUtf8(wchar * pszwDst, int cchwDst, const char * pszSrc);
int SetUtf16FromUtf8(wchar * pszwDst, int cchwDst, const char * rgchSrc, int cchSrc);
long DecodeUtf8(const char * rgchUtf8, int cchUtf8, int & cbOut);

int CountUtf8FromUtf16(const wchar * rgchwSrc, int cchwSrc);
int ConvertUtf16ToUtf8(char * rgchDst, int cchMaxDst, const wchar * rgchwSrc, int cchwSrc);

bool IsAllSpaces(const char * rgch, int cch);

bool ToSurrogate(uint ch32In, wchar* pchOut1, wchar* pchOut2);
bool FromSurrogate(wchar chIn1, wchar pchIn2, uint* pch32Out);

// Returns false if ch is the second (low) of a surrogate pair.
inline bool IsLowSurrogate(wchar ch)
{
	return (ch >= 0xDC00 && ch <= 0xDFFF);
}

// Returns true if ch is the first (high) of a surrogate pair.
inline bool IsHighSurrogate(wchar ch)
{
	return (ch >= 0xD800 && ch < 0xDC00);
}

// Iterates forwards to the next code point; moves past the whole of a surrogate pair.
bool NextCodePoint(int & ich, const wchar * prgch, const int & ichLim);
bool PreviousCodePoint(int & ich, const wchar * prgch);
/*----------------------------------------------------------------------------------------------
	This class is used to convert BYTE streams to and from binhex format. This is used when
	reading and writing XML representations of IMoniker. Since we cannot add XML functionality
	directly to built-in IMoniker interfaces, we use this stream as an intermediary to the
	standard IMoniker Read and Write methods.

	When writing to this stream, it takes byte input data, converts it to binhex format, and
	stores the result in an internal StrAnsi string.

	When reading from this stream, it converts the internal StrAnsi string from binhex format
	to byte data and stores it in the specified array.

	Hungarian: xss.
----------------------------------------------------------------------------------------------*/
class XmlStringStream : public IStream
{
public:
	//:> Static methods
	static void Create(XmlStringStream ** ppxss);

	//:> IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)();
	STDMETHOD_(ULONG, Release)();
	//:> IStream methods
	STDMETHOD(Read)(void * pv, ULONG cb, ULONG * pcbRead);
	STDMETHOD(Write)(const void * pv, ULONG cb, ULONG * pcbWritten);
	STDMETHOD(Seek)(LARGE_INTEGER dlibMove, DWORD dwOrigin, ULARGE_INTEGER * plibNewPosition);
	STDMETHOD(SetSize)(ULARGE_INTEGER libNewSize);
	STDMETHOD(CopyTo)(IStream * pstm, ULARGE_INTEGER cb, ULARGE_INTEGER * pcbRead,
		ULARGE_INTEGER * pcbWritten);
	STDMETHOD(Commit)(DWORD grfCommitFlags);
	STDMETHOD(Revert)();
	STDMETHOD(LockRegion)(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb, DWORD dwLockType);
	STDMETHOD(UnlockRegion)(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb, DWORD dwLockType);
	STDMETHOD(Stat)(STATSTG * pstatstg, DWORD grfStatFlag);
	STDMETHOD(Clone)(IStream ** ppstm);

	// Return the binhex form of the stored data (e.g., 10FFAB23).
	StrAnsi & GetText()
	{
		return m_sta;
	}

protected:
	//:Ignore
	XmlStringStream()
	{
		ModuleEntry::ModuleAddRef();
		m_cref = 1;
	}

	~XmlStringStream()
	{
		ModuleEntry::ModuleRelease();
	}
	//:End Ignore

	//:> Member variables
	long m_cref;
	StrAnsi m_sta;
};


// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkall.bat"
// End: (These 4 lines are useful to Steve McConnel.)

#endif /*UTILXML_H_INCLUDED*/