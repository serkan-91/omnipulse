import { NextResponse } from "next/server";

const BACKEND_URL = "http://localhost:5294";

export async function POST(request: Request) {
  try {
    const body = await request.json();
    
    const backendRes = await fetch(`${BACKEND_URL}/api/auth/login`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(body),
    });

    const data = await backendRes.json();

    if (!backendRes.ok || !data.isSuccess) {
      return NextResponse.json(data, { status: backendRes.status });
    }

    // Extract tokens from backend response
    const { token, refreshToken } = data;

    const response = NextResponse.json({
      isSuccess: true,
      message: "Oturum başarıyla açıldı (BFF)."
    });

    // Set HttpOnly, secure cookies for JWT and Refresh Token 🛡️
    response.cookies.set("op_access_token", token, {
      httpOnly: true,
      secure: process.env.NODE_ENV === "production",
      sameSite: "strict",
      path: "/",
      maxAge: 60 * 60 * 2, // 2 hours (match backend JWT duration)
    });

    if (refreshToken) {
      response.cookies.set("op_refresh_token", refreshToken, {
        httpOnly: true,
        secure: process.env.NODE_ENV === "production",
        sameSite: "strict",
        path: "/",
        maxAge: 60 * 60 * 24 * 7, // 7 days (match backend refresh token duration)
      });
    }

    return response;
  } catch (error: any) {
    console.error("BFF Login Error:", error);
    return NextResponse.json({
      isSuccess: false,
      message: "BFF Giriş Hatası: Sunucu bağlantısı kurulamadı."
    }, { status: 500 });
  }
}
