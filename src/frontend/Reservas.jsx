import { useState, useEffect } from "react";
import { API_BASE_URL } from "./config/api.js";

// Alias para compatibilidade com código existente
const API = API_BASE_URL;

function Reservas() {
  const [reservas, setReservas] = useState([]);
  const [eventos, setEventos] = useState([]);
  const [eventoId, setEventoId] = useState("");
  const [cupom, setCupom] = useState("");
  const [erro, setErro] = useState("");
  const [sucesso, setSucesso] = useState("");
  const [carregando, setCarregando] = useState(false);

  const token = localStorage.getItem("token");

  // Carrega reservas do usuário
  const carregarReservas = async () => {
    setCarregando(true);
    try {
      const res = await fetch(`${API}/api/reservas`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (res.ok) {
        setReservas(await res.json());
      }
    } catch {
      setErro("Erro ao carregar reservas.");
    } finally {
      setCarregando(false);
    }
  };

  // Carrega eventos disponíveis
  const carregarEventos = async () => {
    try {
      const res = await fetch(`${API}/api/eventos/publico`);
      if (res.ok) setEventos(await res.json());
    } catch {}
  };

  useEffect(() => {
    carregarReservas();
    carregarEventos();
  }, []);

  // CRIAR RESERVA
  const criarReserva = async () => {
    setErro("");
    setSucesso("");
    if (!eventoId) {
      setErro("Selecione um evento.");
      return;
    }
    try {
      const res = await fetch(`${API}/api/reservas`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({
          eventoId: parseInt(eventoId),
          codigoCupom: cupom.trim().toUpperCase() || null
        })
      });

      const data = await res.json();

      if (res.ok || res.status === 201) {
        setSucesso(
          `Reserva confirmada! Preço: R$ ${Number(data.precoFinal).toFixed(2)}` +
          (data.cupomAplicado ? ` (cupom ${data.cupomAplicado} aplicado)` : "")
        );
        setEventoId("");
        setCupom("");
        await carregarReservas();
      } else {
        setErro(data || "Erro ao criar reserva.");
      }
    } catch {
      setErro("Erro de conexão com a API.");
    }
  };

  // CANCELAR RESERVA
  const cancelarReserva = async (id) => {
    setErro("");
    setSucesso("");
    if (!window.confirm("Cancelar esta reserva?")) return;
    try {
      const res = await fetch(`${API}/api/reservas/${id}`, {
        method: "DELETE",
        headers: { Authorization: `Bearer ${token}` }
      });
      if (res.ok) {
        setSucesso("Reserva cancelada.");
        await carregarReservas();
      } else {
        const data = await res.text();
        setErro(data || "Erro ao cancelar reserva.");
      }
    } catch {
      setErro("Erro de conexão com a API.");
    }
  };

  const statusCor = (status) => {
    if (status === "confirmada") return "#2ecc71";
    if (status === "cancelada") return "#e74c3c";
    return "#f39c12";
  };

  return (
    <div style={{ padding: "20px", maxWidth: "640px" }}>
      <h1>📋 Minhas Reservas</h1>

      {/* FORMULÁRIO NOVA RESERVA */}
      <div style={{ background: "#f9f9f9", padding: "16px", borderRadius: "8px", marginBottom: "20px" }}>
        <h3>Nova Reserva</h3>

        <select
          value={eventoId}
          onChange={(e) => setEventoId(e.target.value)}
          style={{ display: "block", width: "100%", padding: "8px", marginBottom: "8px" }}
        >
          <option value="">Selecione um evento...</option>
          {eventos.map((e) => (
            <option key={e.id} value={e.id}>
              {e.nome} — R$ {Number(e.precoPadrao).toFixed(2)} ({new Date(e.dataEvento).toLocaleDateString("pt-BR")})
            </option>
          ))}
        </select>

        <input
          placeholder="Cupom de desconto (opcional)"
          value={cupom}
          onChange={(e) => setCupom(e.target.value)}
          style={{ display: "block", width: "100%", padding: "8px", marginBottom: "8px" }}
        />

        {erro && <p style={{ color: "red", margin: "4px 0" }}>{erro}</p>}
        {sucesso && <p style={{ color: "green", margin: "4px 0" }}>{sucesso}</p>}

        <button onClick={criarReserva} style={{ padding: "8px 20px", background: "#3498db", color: "#fff", border: "none", borderRadius: "4px", cursor: "pointer" }}>
          Reservar Ingresso
        </button>
      </div>

      {/* LISTA DE RESERVAS */}
      <h3>Histórico</h3>

      {carregando && <p>Carregando...</p>}

      {!carregando && reservas.length === 0 && (
        <p style={{ color: "#888" }}>Você ainda não tem reservas.</p>
      )}

      {reservas.map((r) => (
        <div key={r.id} style={{ border: "1px solid #ddd", marginTop: "10px", padding: "12px", borderRadius: "6px" }}>
          <p><strong>{r.nomeEvento}</strong></p>
          <p>Data: {new Date(r.dataEvento).toLocaleDateString("pt-BR")}</p>
          <p>Preço pago: <strong>R$ {Number(r.precoFinal).toFixed(2)}</strong></p>
          {r.cupomCodigo && <p>Cupom: {r.cupomCodigo}</p>}
          <p>
            Status:{" "}
            <span style={{ color: statusCor(r.status), fontWeight: "bold" }}>
              {r.status}
            </span>
          </p>
          {r.status !== "cancelada" && (
            <button
              onClick={() => cancelarReserva(r.id)}
              style={{ marginTop: "6px", padding: "6px 12px", background: "#e74c3c", color: "#fff", border: "none", borderRadius: "4px", cursor: "pointer" }}
            >
              Cancelar Reserva
            </button>
          )}
        </div>
      ))}
    </div>
  );
}

export default Reservas;