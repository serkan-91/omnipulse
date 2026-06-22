import { NextResponse } from "next/server";
import { cookies } from "next/headers";

const BACKEND_URL = "http://localhost:5294";

export async function POST() {
  try {
    const cookieStore = await cookies();
    const token = cookieStore.get("op_access_token")?.value;

    if (token) {
      // Call C# Backend logout to revoke JWT immediately in Redis
      await fetch(`${BACKEND_URL}/api/auth/logout`, {
        method: "POST",
        headers: {
          "Authorization": `Bearer ${token}`
        }
      }).catch(err => console.error("Error calling C# logout during BFF logout:", err));
    }

    const response = NextResponse.json({
      isSuccess: true,
      message: "Oturum başarıyla kapatıldı (BFF)."
    });

    // Clear cookies 🧹
    response.cookies.delete("op_access_token");
    response.cookies.delete("op_refresh_token");

    return response;
  } catch (error: any) {
    console.error("BFF Logout Error:", error);
    return NextResponse.json({
      isSuccess: false,
      message: "BFF Çıkış Hatası."
    }, { status: 500 });
  }
}
