import { NextResponse } from "next/server";
import { getOrRefreshAccessToken, parseJwt } from "../utils";

export async function GET() {
  try {
    const token = await getOrRefreshAccessToken();

    if (!token) {
      return NextResponse.json({ isAuthenticated: false }, { status: 401 });
    }

    const payload = parseJwt(token);
    if (!payload) {
      return NextResponse.json({ isAuthenticated: false }, { status: 401 });
    }

    return NextResponse.json({
      isAuthenticated: true,
      userId: payload.sub,
      email: payload.email,
      tenantId: payload.tid,
      tenantIdentifier: payload.tenant_identifier,
      roles: payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || payload.role || []
    });
  } catch (error: unknown) {
    console.error("BFF User Route Error:", error);
    return NextResponse.json({ isAuthenticated: false, message: "Sunucu hatası" }, { status: 500 });
  }
}
