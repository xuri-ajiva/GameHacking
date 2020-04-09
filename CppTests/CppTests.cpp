// HackAnyGamePt2.cpp : Defines the entry point for the console application.
//

#include <iostream>
#include <thread>
#include <vector>
#include <Windows.h>
#include "proc.h"

#define Deg2Rad	 0.0178539
#define POS_ADDRESS 0x109B74
#define MOVE_RANGE 1
#define X_OFFSET {0X80} /*{0x34}*/
#define Y_OFFSET {0x38}
#define Z_OFFSET {0x3C}
#define P_OFFSET {0x40}
#define J_OFFSET {0x44}

DWORD  procId = 0;
HWND   hwnd   = nullptr;
HANDLE hProcess;
HHOOK  hhkLowLevelKybd;

uintptr_t A_X;
uintptr_t A_Y;
uintptr_t A_Z;
uintptr_t A_P;
uintptr_t A_J;

float X_v = 0;
float Y_v = 0;
float Z_v = 0;
float P_v = 0;
float J_v = 0;

struct vec2 {
	float x, y;
public:
	void scale(float f) {
		x *= f;
		y *= f;
	};

	void normalize() {
		auto len = length();

		if (len > 0) {
			scale(1 / len);
		}
	}

	float length() {
		return sqrt(x * x + y * y);
	}
};

struct vec3 {
	float x, y, z;
public:
	void scale(float f) {
		x *= f;
		y *= f;
		z *= f;
	};

	void normalize() {
		auto len = length();

		if (len > 0) {
			scale(1 / len);
		}
	}

	float length() {
		return sqrt(x * x + y * y + z * z);
	}
};

bool EXit_TX = false;
bool Fly     = false;

LRESULT CALLBACK LowLevelKeyboardProc(int nCode, WPARAM wParam, LPARAM lParam) {
	auto fEatKeystroke = FALSE;
	auto p             = (PKBDLLHOOKSTRUCT)lParam;
	//std::cout << char(p->vkCode);
	//std::cout << char(p->vkCode) << (p->vkCode) << "\n";

	vec3 m = {X_v, Y_v, Z_v};
	vec3 c = {X_v, Y_v, Z_v};


	//if (nCode == HC_ACTION) {
	//	switch (wParam) {
	//		case WM_KEYDOWN:
	//		case WM_SYSKEYDOWN:
	//			break;
	//		case WM_KEYUP:
	//		case WM_SYSKEYUP:
	//			if ((p->vkCode == 33)) {
	//				printf("up\n");
	//				m.z += MOVE_RANGE;
	//			} else if ((p->vkCode == 36)) {
	//				printf("down\n");
	//				m.z -= MOVE_RANGE;
	//			} else if ((p->vkCode == 38)) {
	//				printf("for\n");
	//				m.x += MOVE_RANGE;
	//			} else if ((p->vkCode == 40)) {
	//				printf("back\n");
	//				m.x -= MOVE_RANGE;
	//			} else if ((p->vkCode == 39)) {
	//				printf("right\n");
	//				m.y -= MOVE_RANGE;
	//			} else if ((p->vkCode == 37)) {
	//				printf("left\n");
	//				m.y += MOVE_RANGE;
	//			} else if ((p->vkCode == 35)) {
	//				printf("Exit\n");
	//				EXit_TX = true;
	//				printf("UnhookWindowsHookEx");
	//				UnhookWindowsHookEx(hhkLowLevelKybd);
	//				Sleep(150);
	//				exit(0);
	//			} else if ((p->vkCode == VK_DELETE)) {
	//				Fly = ! Fly;
	//				std::cout << "Fly: " << Fly << "\n";
	//			}
	//	}
	//}

	if (procId != 0 && A_X != 0 && A_Y != 0 && A_Z != 0) {			  
		m.normalize();


		

		float _x = m.x;
		float _y = m.y;

		float _angle = Deg2Rad* P_v;
		float _cos   = cos(_angle);
		float _sin   = sin(_angle);

		float _x2 = _x * _cos - _y * _sin;
		float _y2 = _x * _sin + _y * _cos;

		m.x =  _x2 * MOVE_RANGE;
		m.y =  _y2 * MOVE_RANGE;

		if(GetAsyncKeyState('W')) {
			m.x*= 10;
		} else if(GetAsyncKeyState('D')) {
			m.y*= 10;
		}
		else if(GetAsyncKeyState('A')) {
			m.y *= -10;
		}
		else if(GetAsyncKeyState('S')) {
			m.x*= -10;
		}
		
		std::cout << _x2 <<"\n";
		std::cout << _y2 <<"\n";

		m.x += c.x;
		m.y += c.y;
		
		//m.y *= a.x;
		//m.x *= a.y;
		if (Fly) Z_v = m.z;

		if (m.x != c.x) WriteProcessMemory(hProcess, (BYTE*)A_X, &m.x, sizeof(float), nullptr);
		if (m.y != c.y) WriteProcessMemory(hProcess, (BYTE*)A_Y, &m.y, sizeof(float), nullptr);
		//if (m.z != c.z) WriteProcessMemory(hProcess, (BYTE*)A_Z, &m.z, sizeof(float), nullptr);
	}

	return (fEatKeystroke ? 1 : CallNextHookEx(NULL, nCode, wParam, lParam));
}

void keyBord() {
	std::cout << "Init KeyBord Hock!" << std::endl;
	// Install the low-level keyboard & mouse hooks
	hhkLowLevelKybd = SetWindowsHookEx(WH_KEYBOARD_LL, LowLevelKeyboardProc, 0, 0);

	// Keep this app running until we're told to stop
	MSG msg;
	while (!GetMessage(&msg, hwnd, NULL, NULL)) {
		TranslateMessage(&msg);
		DispatchMessage(&msg);
	}

	printf("UnhookWindowsHookEx");
	UnhookWindowsHookEx(hhkLowLevelKybd);
}

void printAll() {
	std::cout << "X = " << std::dec << X_v << std::endl;
	std::cout << "Y = " << std::dec << Y_v << std::endl;
	std::cout << "Z = " << std::dec << Z_v << std::endl;
	std::cout << "P = " << std::dec << P_v << std::endl;
	std::cout << "J = " << std::dec << J_v << std::endl;
	std::cout << "FLY = " << std::dec << Fly << std::endl;
	std::cout << std::endl << std::endl;
}

int main() {
	while (procId == 0) {
		Sleep(500);
		printf("Waiting for process\n");
		procId = GetProcId(L"svencoop.exe"); //L"ac_client.exe");
	}

	//auto HasWnd = false;
	//while (!HasWnd) {
	//	Sleep(100);
	//	printf("Waiting for WindowFocus\n");
	//	HWND hw = GetForegroundWindow();
	//	DWORD procID;
	//	GetWindowThreadProcessId(hwnd, &procID);
	//	if (procID == procId) {
	//		HasWnd = true;
	//		hwnd = hw;
	//	}  
	//}


	Sleep(100);
	std::thread first(keyBord);
	Sleep(100);

	//Getmodulebaseaddress
	const auto moduleBase = GetModuleBaseAddress(procId, L"hw.dll");//L"ac_client.exe");

	//Get Handle to Process
	hProcess = OpenProcess(PROCESS_ALL_ACCESS, NULL, procId);

	//Resolve base address of the pointer chain
	const auto positionPtrBaseAddr = moduleBase + POS_ADDRESS;

	std::cout << "DynamicPtrBaseAddr = " << "0x" << std::hex << positionPtrBaseAddr << std::endl;


	A_X = FindDMAAddy(hProcess, positionPtrBaseAddr,X_OFFSET);
	A_Y = FindDMAAddy(hProcess, positionPtrBaseAddr, Y_OFFSET);
	A_Z = FindDMAAddy(hProcess, positionPtrBaseAddr, Z_OFFSET);
	A_P = FindDMAAddy(hProcess, positionPtrBaseAddr, P_OFFSET);
	A_J = FindDMAAddy(hProcess, positionPtrBaseAddr, J_OFFSET);


	std::cout << "X = " << "0x" << std::hex << A_X << std::endl;
	std::cout << "Y = " << "0x" << std::hex << A_Y << std::endl;
	std::cout << "Z = " << "0x" << std::hex << A_Z << std::endl;
	std::cout << "P = " << "0x" << std::hex << A_P << std::endl;
	std::cout << "J = " << "0x" << std::hex << A_J << std::endl;


	float X_v_T = 0;
	float Y_v_T = 0;
	float Z_v_T = 0;
	float P_v_T = 0;
	float J_v_T = 0;


	HANDLE hConsole = GetStdHandle(STD_OUTPUT_HANDLE);
	while (!EXit_TX) {
		Sleep(1);


		ReadProcessMemory(hProcess, (BYTE*)A_X, &X_v_T, sizeof(float), nullptr);
		if (X_v_T != X_v) {
			X_v = X_v_T;
		}

		ReadProcessMemory(hProcess, (BYTE*)A_Y, &Y_v_T, sizeof(float), nullptr);
		if (Y_v_T != Y_v) {
			Y_v = Y_v_T;
		}
		if (Fly) {
			WriteProcessMemory(hProcess, (BYTE*)A_Z, &Z_v, sizeof(float), nullptr);
		} else {
			ReadProcessMemory(hProcess, (BYTE*)A_Z, &Z_v_T, sizeof(float), nullptr);
			if (Z_v_T != Z_v) {
				Z_v = Z_v_T;
			}
		}

		ReadProcessMemory(hProcess, (BYTE*)A_P, &P_v_T, sizeof(float), nullptr);
		if (P_v_T != P_v) {
			P_v = P_v_T;
		}
		ReadProcessMemory(hProcess, (BYTE*)A_J, &J_v_T, sizeof(float), nullptr);
		if (J_v_T != J_v) {
			J_v = J_v_T;
		}


		COORD pos = {0, 10};
		SetConsoleCursorPosition(hConsole, pos);
		//printAll();
	}

	Sleep(400);
	//auto ammoValue = 0;
	//
	////Write to it
	//auto newAmmo = 1337;
	//WriteProcessMemory(hProcess, (BYTE*)A_X, &newAmmo, sizeof(newAmmo), nullptr);
	//
	////Read out again
	//ReadProcessMemory(hProcess, (BYTE*)A_X, &ammoValue, sizeof(ammoValue), nullptr);
	//
	//std::cout << "New ammo = " << std::dec << ammoValue << std::endl;


	return 0;
}
