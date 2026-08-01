import os
import json
import pytest
from unittest.mock import patch, mock_open

# Importações do monolito e da camada core original
from anubis import calcular_score, judgment
from core.utils import get_headers, get_proxies, set_tor, is_tor
from core.config import config_load, config_save, DEFAULT_CONFIG

# ==========================================
# TESTES: core/utils.py
# ==========================================

def test_get_headers():
    """Garante que a função sempre retorna um User-Agent válido da lista."""
    headers = get_headers()
    assert "User-Agent" in headers
    assert isinstance(headers["User-Agent"], str)
    assert len(headers["User-Agent"]) > 10

def test_tor_mode():
    """Testa o comportamento de toggle da configuração de proxy TOR."""
    # Habilitando modo TOR
    set_tor(True)
    assert is_tor() is True
    proxies = get_proxies()
    assert "http" in proxies
    assert "socks5h://127.0.0.1:9150" == proxies["http"]
    
    # Desabilitando modo TOR
    set_tor(False)
    assert is_tor() is False
    assert get_proxies() == {}

# ==========================================
# TESTES: core/config.py
# ==========================================

@patch("os.path.exists")
@patch("core.config.config_save")
def test_config_load_not_exists(mock_save, mock_exists):
    """Testa o carregamento de config quando o arquivo NÃO existe (gera o DEFAULT)."""
    mock_exists.return_value = False
    cfg = config_load()
    
    assert cfg == DEFAULT_CONFIG
    mock_save.assert_called_once_with(DEFAULT_CONFIG)

@patch("os.path.exists")
@patch("builtins.open", new_callable=mock_open, read_data='{"timeout": 10, "apis": {}}')
def test_config_load_exists(mock_file, mock_exists):
    """Testa o carregamento de config simulando a leitura do JSON."""
    mock_exists.return_value = True
    cfg = config_load()
    
    assert cfg["timeout"] == 10
    mock_file.assert_called_once()

@patch("os.makedirs")
@patch("builtins.open", new_callable=mock_open)
def test_config_save(mock_file, mock_makedirs):
    """Testa a gravação segura no disco garantindo que os diretórios sejam criados e o arquivo escrito."""
    fake_data = {"test_key": "test_value"}
    config_save(fake_data)
    
    mock_makedirs.assert_called_once()
    mock_file.assert_called_once()
    
    # Extrai todo o texto que foi chamado via write()
    handle = mock_file()
    written_content = "".join(call.args[0] for call in handle.write.call_args_list)
    assert '"test_key": "test_value"' in written_content

# ==========================================
# TESTES: anubis.py (Business Rules / Score)
# ==========================================

@pytest.mark.parametrize("portas, high, medium, sub, ssl, perfis, cves, expected", [
    # Boundary Mínimo Absoluto: Nenhum risco (Tudo zero / SSL em dia)
    (0, 0, 0, 0, 90, 0, 0, 0),
    
    # Testando limites individuais de cada métrica antes do Cap/Max:
    (1, 1, 1, 1, 100, 1, 1, 35), # 5 + 15 + 5 + 2 + 0 + 3 + 5 = 35
    
    # Boundary: Atingindo exatamente o teto de pontuação de cada item
    (6, 3, 4, 10, 100, 5, 5, 100), # Na verdade, antes da limitação global é 150. Porém o retorno é min(score, 100) -> 100
    
    # Boundary do limitador global: A pontuação total NÃO pode passar de 100
    (100, 100, 100, 100, -1, 100, 100, 100),
    
    # Testes de Regra de Negócio Específica (SSL Dias)
    (0, 0, 0, 0, None, 0, 0, 20), # Sem SSL -> Risco Máximo
    (0, 0, 0, 0, -5,   0, 0, 20), # SSL Expirado -> Risco Máximo
    (0, 0, 0, 0, 29,   0, 0, 10), # SSL Expira em menos de 30 dias -> Risco Médio
    (0, 0, 0, 0, 30,   0, 0, 0),  # SSL Ok -> Zero risco
])
def test_calcular_score(portas, high, medium, sub, ssl, perfis, cves, expected):
    """Explora a matemática dos scores de ameaça usando matriz de parametrização Boundary/Equivalence."""
    score = calcular_score(portas, high, medium, sub, ssl, perfis, cves)
    assert score == expected
    assert 0 <= score <= 100 # Invariante de Segurança

def test_judgment_verdicts(capsys):
    """Testa se o Output final para o usuário bate com as faixas de Overall (Score 0-100)."""
    
    # Boundary CONDEMNED (75-100)
    judgment(100, 100, 100, None) 
    captured = capsys.readouterr()
    assert "CONDEMNED" in captured.out
    
    # Boundary WATCH CLOSELY (50-74)
    judgment(60, 50, 40, None) # AVG = 50
    captured = capsys.readouterr()
    assert "WATCH CLOSELY" in captured.out
    
    # Boundary MINOR SINS (25-49)
    judgment(30, 20, 25, None) # AVG = 25
    captured = capsys.readouterr()
    assert "MINOR SINS" in captured.out
    
    # Boundary SOUL IS CLEAN (0-24)
    judgment(10, 10, 10, None) # AVG = 10
    captured = capsys.readouterr()
    assert "SOUL IS CLEAN" in captured.out
