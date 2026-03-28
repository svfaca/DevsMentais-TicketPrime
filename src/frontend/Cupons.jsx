import { useState, useEffect } from "react";

const API = (() => {
  const configuredUrl = window.localStorage.getItem("API_URL");
  if (configuredUrl) return configuredUrl.replace(/\/+$/, "");
  if (window.location.hostname === "localhost") return "http://localhost:5129";
  return "https://devsmentais-ticketprime.up.railway.app";
})();

function Cupons() {
  const [codigo, setCodigo] = useState("");
  const [desconto, setDesconto] = useState("");
  const [valorMinimo, setValorMinimo] = useState("");
  const [cupons, setCupons] = useState([]);
  const [erro, setErro] = useState("");
  const [carregando, setCarregando] = useState(false);

  const token = localStorage.getItem("token");

  // LISTAR
  const carregarCupons = async () => {
    setCarregando(true);
    try {
      const res = await fetch(`${API}/api/cupons`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (res.ok) {
        const data = await res.json();
        setCupons(data);
      } else {
        setErro("Erro ao carregar cupons.");
      }
    } catch {
      setErro("Erro de conexão com a API.");
    } finally {
      setCarregando(false);
    }
  };

  useEffect(() => {
    carregarCupons();
  }, []);

  // CREATE
  const criarCupom = async () => {
    setErro("");
    if (!codigo || !desconto || !valorMinimo) {
      setErro("Preencha todos os campos.");
      return;
    }
    try {
      const res = await fetch(`${API}/api/cupons`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({
          codigo: codigo.toUpperCase(),
          porcentagemDesconto: parseFloat(desconto),
          valorMinimoRegra: parseFloat(valorMinimo)
        })
      });
      if (res.ok || res.status === 201) {
        setCodigo("");
        setDesconto("");
        setValorMinimo("");
        await carregarCupons();
      } else {
        const msg = await res.text();
        setErro(msg || "Erro ao criar cupom.");
      }
    } catch {
      setErro("Erro de conexão com a API.");
    }
  };

  return (
    <div style={{ padding: "20px", maxWidth: "600px" }}>
      <h1>🎟️ Gerenciar Cupons</h1>

      <input
        placeholder="Código (ex: DESC10)"
        value={codigo}
        onChange={(e) => setCodigo(e.target.value)}
        style={{ display: "block", marginBottom: "8px", width: "100%", padding: "6px" }}
      />
      <input
        placeholder="Desconto (%)"
        type="number"
        value={desconto}
        onChange={(e) => setDesconto(e.target.value)}
        style={{ display: "block", marginBottom: "8px", width: "100%", padding: "6px" }}
      />
      <input
        placeholder="Valor mínimo do ingresso (R$)"
        type="number"
        value={valorMinimo}
        onChange={(e) => setValorMinimo(e.target.value)}
        style={{ display: "block", marginBottom: "8px", width: "100%", padding: "6px" }}
      />

      {erro && <p style={{ color: "red" }}>{erro}</p>}

      <button onClick={criarCupom} style={{ padding: "8px 16px" }}>
        Criar Cupom
      </button>

      <hr />
      <h3>Lista de Cupons</h3>

      {carregando && <p>Carregando...</p>}

      {!carregando && cupons.length === 0 && (
        <p style={{ color: "#888" }}>Nenhum cupom cadastrado.</p>
      )}

      {cupons.map((c) => (
        <div key={c.codigo} style={{ border: "1px solid #ccc", marginTop: "10px", padding: "10px", borderRadius: "6px" }}>
          <p><strong>{c.codigo}</strong></p>
          <p>Desconto: {c.porcentagemDesconto}%</p>
          <p>Valor mínimo: R$ {Number(c.valorMinimoRegra).toFixed(2)}</p>
        </div>
      ))}
    </div>
  );
}

export default Cupons;