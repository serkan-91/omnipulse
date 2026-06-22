import { NextResponse } from "next/server";
import { getOrRefreshAccessToken } from "../utils";

export async function GET() {
  try {
    const token = await getOrRefreshAccessToken();

    if (!token) {
      return NextResponse.json({ token: null }, { status: 401 });
    }

    // Return the token to the client *only* for the in-memory WebSocket connection setup 🛡️
    return NextResponse.json({ token });
  } catch (error) {
    console.error("BFF WS Token Route Error:", error);
    return NextResponse.json({ token: null }, { status: 500 });
  }
}
