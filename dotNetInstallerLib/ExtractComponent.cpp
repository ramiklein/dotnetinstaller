#include "StdAfx.h"
#include "ExtractComponent.h"
#include "InstallerLog.h"
#include "InstallerSession.h"

ExtractComponent::ExtractComponent(HMODULE h)
	: m_h(h)
	, cancelled(false)
{

}


int ExtractComponent::ExecOnThread()
{
	ResolvePaths();
	WriteCab();
    ExtractCab();
	LOG(L"ExtractComponent: extracted Setup.cab");
	return 0;
}

int ExtractComponent::GetCabCount() const
{
	int currentIndex = 1;	
	std::wstring resname = TEXT("RES_CAB");
	resname.append(DVLib::towstring(currentIndex));
	
	HRSRC l_res = FindResource(m_h, resname.c_str(), TEXT("RES_CAB"));
	if (l_res == NULL)
		return 0;

	do
	{
		currentIndex++;
		resname = TEXT("RES_CAB");
		resname.append(DVLib::towstring(currentIndex));
		l_res = FindResource(m_h, resname.c_str(), TEXT("RES_CAB"));
	} while(l_res);

	return currentIndex - 1;
}

void ExtractComponent::ResolvePaths()
{
    LOG(L"Extracting Setup.cab");

	resolved_cab_path = cab_path.empty() ? InstallerSession::Instance->GetSessionTempPath() : cab_path; 
	resolved_cab_path = InstallerSession::Instance->ExpandVariables(resolved_cab_path);
	LOG(L"Cabpath: " << resolved_cab_path);
	DVLib::DirectoryCreate(resolved_cab_path);

    resolved_cab_file = DVLib::DirectoryCombine(resolved_cab_path, TEXT("setup.cab"));
	LOG(L"Cabfile: " << resolved_cab_file);
}

void ExtractComponent::WriteCab()
{
	ULONG currentIndex = 1;
	std::wstring resname = TEXT("RES_CAB");
	resname.append(DVLib::towstring(currentIndex));

	auto_hfile hfile(CreateFile(resolved_cab_file.c_str(), GENERIC_WRITE, FILE_SHARE_READ, NULL, 
		OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL));

	CHECK_WIN32_BOOL(get(hfile) != INVALID_HANDLE_VALUE,
		L"Error creating '" << resolved_cab_file << L"'");

	int cabCount = GetCabCount();
	DWORD dwWrittenTotal = 0;

	for (int i = 1; i <= cabCount; i++)
	{
		OnStatus(L"Setup.cab - " + DVLib::FormatMessage(L"%d%%", (i * 100) / cabCount));

		std::wstring resname = TEXT("RES_CAB");
		resname.append(DVLib::towstring(i));
		LOG(L"Extracting: " << resname);

        if (cancelled)
        {
			LOG(L"Cancelled: " << resname);
            std::wstring resolved_cancelled_message = cab_cancelled_message;
            if (resolved_cancelled_message.empty()) resolved_cancelled_message = L"Cancelled by user";
			THROW_EX(resolved_cancelled_message);
        }

		std::vector<char> data = DVLib::LoadResourceData<char>(m_h, resname, L"RES_CAB");

		DWORD dwWritten = 0;
        CHECK_WIN32_BOOL(WriteFile(get(hfile), (LPCVOID) & * data.begin(), data.size(), & dwWritten, NULL),
			L"Error writing setup.cab at '" << resname << L"' resource");

		dwWrittenTotal += dwWritten;
		LOG(L"Extracted: " << resname);
    }

	LOG(L"Extracted " << DVLib::FormatBytesW(dwWrittenTotal) << L" from " << cabCount << L" resource segment(s)");
}

void ExtractComponent::ExtractCab()
{
	CHECK_BOOL(CreateFDIContext(),
		L"Error initializing CAB context");

	CHECK_BOOL(ExtractFileW(const_cast<wchar_t *>(resolved_cab_file.c_str()), 
		const_cast<wchar_t *>(resolved_cab_path.c_str())), L"Error extracting '" << resolved_cab_file << L"'");

	LOG(L"Extracted CAB: " << resolved_cab_file);
}

void ExtractComponent::OnAfterCopyFile(char* s8_File, WCHAR* u16_File, void* p_Param)
{
	LOG(L"Done: " << u16_File);
    Cabinet::CExtractT<ExtractComponent>::OnAfterCopyFile(s8_File, u16_File, p_Param);
}

BOOL ExtractComponent::OnBeforeCopyFile(kCabinetFileInfo &k_FI, void* p_Param)
{
	LOG(L"Extracting: " << k_FI.u16_FullPath);

	OnStatus(std::wstring(k_FI.u16_File) + L" - " + DVLib::FormatBytesW(k_FI.s32_Size));

	std::wstring cancelled_message;
	if (cancelled)
    {
        std::wstring resolved_cancelled_message = cab_cancelled_message;
        if (resolved_cancelled_message.empty()) resolved_cancelled_message = L"Cancelled by user";
        THROW_EX(resolved_cancelled_message);
    }

    return Cabinet::CExtractT<ExtractComponent>::OnBeforeCopyFile(k_FI, p_Param);
}

std::vector<std::wstring> ExtractComponent::GetCabFiles() const
{
	std::vector<wchar_t> v_buffer = DVLib::LoadResourceData<wchar_t>(m_h, L"RES_CAB_LIST", L"CUSTOM");
	std::wstring s_buffer(& * v_buffer.begin(), v_buffer.size());
	return DVLib::split(s_buffer, L"\r\n");
}
