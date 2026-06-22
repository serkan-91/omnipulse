import { NextResponse } from "next/server";

if (process.env.NODE_ENV === "development") {
  process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0";
}

const BACKEND_URL = "https://localhost:7122";

export async function POST(request: Request) {
  try {
    const body = await request.json();

    const backendRes = await fetch(`${BACKEND_URL}/api/tenants/register`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(body),
    });

    const data = await backendRes.json();

    if (!backendRes.ok || !data.isSuccess) {
      return NextResponse.json(data, { status: backendRes.status || 400 });
    }

    const { token } = data;

    const response = NextResponse.json({
      isSuccess: true,
      message: data.message || "Şirket kaydı başarıyla tamamlandı (BFF)."
    });

    if (token) {
      // Set HttpOnly, secure cookie for JWT 🛡️
      response.cookies.set("op_access_token", token, {
        httpOnly: true,
        secure: process.env.NODE_ENV === "production",
        sameSite: "strict",
        path: "/",
        maxAge: 60 * 60 * 2, // 2 hours
      });
    }

    return response;
  } catch (error: unknown) {
    console.error("BFF Register Error:", error);
    return NextResponse.json({
      isSuccess: false,
      message: "BFF Kayıt Hatası: Sunucu bağlantısı kurulamadı."
    }, { status: 500 });
  }
}
