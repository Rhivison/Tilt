#include <Wire.h>
#include <Nanoshield_ADC.h>
#include <Nanoshield_Ethernet.h>
#include <SPI.h>

// === CONFIGURAÇÕES MOTOR NEMA KTC-HT23-401 + DRIVER STR8 ===
// Correção da constante: Motor com 200 passos/rev (1,8° por passo)
const float PASSOS_POR_REVOLUCAO = 200.0;  // 1.8° por passo
const float MICROSTEPPING = 400.0;          // driver STR8
const float REDUCAO_MECANICA = 40.0;        // redução 1:40
const float CORRECAO_VELOCIDADE = 3.4;
const float GRAUS_POR_PASSO = (360.0 / (PASSOS_POR_REVOLUCAO * MICROSTEPPING * REDUCAO_MECANICA))*CORRECAO_VELOCIDADE; 

bool modoReposicionamentoAtivo = false;


//const float GRAUS_POR_PASSO = 0.0225;
// === CONTROLE DE VELOCIDADE OTIMIZADO ===
float velocidadeDesejada = 30.0;  // Graus por minuto (padrão)
float velocidadeMinima = 0.1;     // Graus/min mínima
float velocidadeMaxima = 100.0;    // Graus/min máxima (ajustada para alta precisão)
volatile uint16_t timerValue = 37;

// === Variáveis de ensaio ===
const int MAX_ANGULOS = 200;
float angulosEnsaio[MAX_ANGULOS];
unsigned long tempoEnsaio[MAX_ANGULOS];
int totalAngulos = 0;
float anguloFiltrado = 0.0;
const float alpha = 0.2;

// === PINOS ===
const int stepPin = 5;
const int dirPin = 6;
const int BOTAOVERDE = 7;
const int BOTAOVERMELHO = 8;
const int emergencia = 9;
const int fc1 = 2;  // FC sentido vermelho
const int fc2 = 4;  // FC sentido verde
const int sensor = A3;
const int sensorPin = A1;
const int ETHERNET_CS = A2;

// === Estados e controle ===
bool sentido1 = false, sentido2 = false;
bool sensorAtivado = false;
bool f1 = false, f2 = false;
bool motorAtivo = false;
bool stepState = false;
bool estadoEmergencia = false;
int sensorValue = 0;
bool modoEnsaioAtivo = false;
EthernetClient client;



// === Ethernet ===
byte mac[] = { 0xDE, 0xAD, 0xBE, 0xEF, 0xFE, 0xED };
IPAddress ipFixo(192, 168, 0, 200);
IPAddress gateway(192, 168, 0, 1);
IPAddress subnet(255, 255, 255, 0);
IPAddress dnsServer(8, 8, 8, 8);
EthernetServer server(5000);
bool controleRemotoAtivo = false;
bool ethernetConectado = false;
bool useDhcp = false;
// === ADC ===
Nanoshield_ADC adc;
int channel = 0;

// === Controle de tempo ===
unsigned long ultimaLeitura = 0;
const unsigned long intervaloLeitura = 200;
unsigned long ultimoTestePing = 0;
const unsigned long intervaloTestePing = 5000;

// === VARIÁVEIS DE CALIBRAÇÃO ===
bool modoCalibracao = false;
unsigned long ultimoEnvioCalibracaoTcp = 0;
const unsigned long intervaloEnvioCalibracaoTcp = 100; // Envia a cada 100ms durante calibração

// === FUNÇÕES DE CONTROLE DE VELOCIDADE CORRIGIDAS ===

float aplicarFiltroExponencial(float novoValor) {
  anguloFiltrado = alpha * novoValor + (1 - alpha) * anguloFiltrado;
  return anguloFiltrado;
}

// Calcula passos por segundo baseado na velocidade em graus/min
float calcularPassosPorSegundo(float grausPorMinuto) {
  if (grausPorMinuto <= 0) return 0.0;
  float grausPorSegundo = grausPorMinuto / 60.0;
  float passosPorSegundo = grausPorSegundo / GRAUS_POR_PASSO;
  return passosPorSegundo;
}

// Calcula valor do timer baseado na velocidade desejada
// Função para calcular o valor do timer baseado na velocidade desejada
uint16_t calcularTimerValue(float grausPorMinuto) {
  grausPorMinuto = constrain(grausPorMinuto, velocidadeMinima, velocidadeMaxima);
  
  float passosPorSegundo = calcularPassosPorSegundo(grausPorMinuto);

  if (passosPorSegundo <= 0) {
    // Se velocidade zero ou menor, coloca timer no máximo para parar pulsos
    return 65535;
  }

  // Frequência do timer deve ser 2x a frequência de passos (toggle do pino step)
  float frequenciaTimer = passosPorSegundo * 2.0;

  // OCR1A = (F_CPU / (prescaler * frequencia)) - 1
  uint16_t timerVal = (16000000UL / (64UL * frequenciaTimer)) - 1;

  // Limita valores do timer
  timerVal = constrain(timerVal, 1, 65535);

  return timerVal;
}

// Define nova velocidade em graus por minuto
void setVelocidade(float grausPorMinuto) {
  velocidadeDesejada = constrain(grausPorMinuto, velocidadeMinima, velocidadeMaxima);
  timerValue = calcularTimerValue(velocidadeDesejada);
  
  // Atualiza timer atomicamente
  noInterrupts();
  OCR1A = timerValue;
  interrupts();
  
  // Calcula valores para debug
  float passosPorSeg = calcularPassosPorSegundo(velocidadeDesejada);
  float frequenciaTimer = passosPorSeg * 2.0;
  
  Serial.print("✅ Velocidade: ");
  Serial.print(velocidadeDesejada, 3);
  Serial.print(" graus/min | Passos/seg: ");
  Serial.print(passosPorSeg, 3);
  Serial.print(" | Freq Timer: ");
  Serial.print(frequenciaTimer, 2);
  Serial.print(" Hz | Timer: ");
  Serial.println(timerValue);
}

// Incrementa velocidade
void incrementarVelocidade(float incremento = 1.0) {
  setVelocidade(velocidadeDesejada + incremento);
}

// Decrementa velocidade
void decrementarVelocidade(float decremento = 1.0) {
  setVelocidade(velocidadeDesejada - decremento);
}

// Mostra informações técnicas do motor
void mostrarInfoMotor() {
  Serial.println("=== CONFIGURAÇÃO MOTOR NEMA + STR8 ===");
  Serial.print("Passos por revolução: ");
  Serial.println(PASSOS_POR_REVOLUCAO, 0);
  Serial.print("Microstepping STR8: ");
  Serial.println(MICROSTEPPING, 0);
  Serial.print("Redução mecânica: 1:");
  Serial.println(REDUCAO_MECANICA, 0);
  Serial.print("Graus por microstep: ");
  Serial.println(GRAUS_POR_PASSO, 6);
  Serial.print("Total microsteps/revolução: ");
  Serial.println(PASSOS_POR_REVOLUCAO * MICROSTEPPING * REDUCAO_MECANICA, 0);
  Serial.print("Velocidade mín/máx: ");
  Serial.print(velocidadeMinima, 1);
  Serial.print("/");
  Serial.print(velocidadeMaxima, 1);
  Serial.println(" graus/min");
  Serial.println("=====================================");
}

// === Setup Timer1 para controle preciso ===
void setupTimer1() {
  noInterrupts();
  
  // Limpa registradores
  TCCR1A = 0;
  TCCR1B = 0;
  TCNT1 = 0;
  
  // Configura CTC mode (Clear Timer on Compare)
  TCCR1B |= (1 << WGM12);
  
  // Configura prescaler 64 (CS11 | CS10)
  TCCR1B |= (1 << CS11) | (1 << CS10);
  
  // Calcula e define valor inicial do timer
  timerValue = calcularTimerValue(velocidadeDesejada);
  OCR1A = timerValue;
  
  // Habilita interrupção de comparação
  TIMSK1 |= (1 << OCIE1A);
  
  interrupts();
  
  Serial.print("Timer1 configurado - Valor: ");
  Serial.print(timerValue);
  Serial.print(" para ");
  Serial.print(velocidadeDesejada);
  Serial.println(" graus/min");
}

// === Controle do motor ===
void controleMotor() {
  // Atualiza estado dos sensores e botões

  if (!controleRemotoAtivo) {
    sentido1 = !digitalRead(BOTAOVERDE);
    sentido2 = !digitalRead(BOTAOVERMELHO);
  }

  estadoEmergencia = digitalRead(emergencia); // LOW = acionado
  f1 = !digitalRead(fc1); // true se fim de curso pressionado
  f2 = !digitalRead(fc2);
  sensorValue = analogRead(sensorPin);

  const bool ignorarSeguranca = false;  // <<< Troque para true SOMENTE para testes

   if (modoReposicionamentoAtivo) {
    if (!estadoEmergencia && f1) {
      digitalWrite(dirPin, HIGH); // Sentido do botão vermelho (sentido2)
      motorAtivo = true;
    } else {
      motorAtivo = false;
      modoReposicionamentoAtivo = false;
    }
    return; // Ignora os outros modos (manual/ensaio)
  }
  // === Ensaio automático ativo ===
    if (modoEnsaioAtivo) {
    if (sensorValue != 0 && (f2 || ignorarSeguranca) && (!estadoEmergencia || ignorarSeguranca)) {
      digitalWrite(dirPin, LOW); // Sentido 2
      motorAtivo = true;
    } else {
      // Finaliza o ensaio
      motorAtivo = false;
      modoEnsaioAtivo = false;
      controleRemotoAtivo = false;
      
      // Envia dados finais e sinaliza fim
      if (client && client.connected()) {
        client.println("ENSAIO_FINALIZADO");
        Serial.println("⚠️ ENSAIO FINALIZADO");
      }
    }
  }
  // === Controle manual ou remoto ===
  else {
    if (sentido1 && !sentido2) {
      if ((!estadoEmergencia && f2) || ignorarSeguranca) {
        digitalWrite(dirPin, LOW);  // Sentido 1
        motorAtivo = true;
      } else {
        motorAtivo = false;
        //Serial.println("⛔ Bloqueado - Fim de curso 2 ou emergência no sentido 1");
      }
    } else if (sentido2 && !sentido1) {
      if ((!estadoEmergencia && f1) || ignorarSeguranca) {
        digitalWrite(dirPin, HIGH); // Sentido 2
        motorAtivo = true;
      } else {
        motorAtivo = false;
        //Serial.println("⛔ Bloqueado - Fim de curso 1 ou emergência no sentido 2");
      }
    } else {
      motorAtivo = false;
    }
  }

}

// === Ethernet - Diagnóstico ===
void diagnosticarEthernet() {
  Serial.println("=== DIAGNÓSTICO ETHERNET ===");
  
  pinMode(ETHERNET_CS, OUTPUT);
  digitalWrite(ETHERNET_CS, HIGH);
  SPI.begin();
  
  for (int tentativa = 1; tentativa <= 3; tentativa++) {
    Serial.print("Tentativa ");
    Serial.print(tentativa);
    Serial.print("/3: ");
    
    if (useDhcp) {
      if (Ethernet.begin(mac)) {
        Serial.println("DHCP OK!");
        ethernetConectado = true;
        break;
      }
    }
    
    Ethernet.begin(mac, ipFixo, dnsServer, gateway, subnet);
    delay(2000);
    
    if (Ethernet.localIP() != IPAddress(0, 0, 0, 0)) {
      Serial.println("IP fixo OK!");
      ethernetConectado = true;
      break;
    }
    
    Serial.println("FALHOU");
    delay(2000);
  }
  
  if (ethernetConectado) {
    Serial.print("IP: ");
    Serial.println(Ethernet.localIP());
  }
}

void enviarDadosCalibracaoTcp(){
  if (!modoCalibracao || !client || !client.connected()) return;
  if (millis() - ultimoEnvioCalibracaoTcp >= intervaloEnvioCalibracaoTcp){
    float corrente_mA = adc.read4to20mA(channel);
    //float angulo = ((corrente_mA - 12.0) / 8.0) * 90.0;
    //angulo = constrain(angulo, -90.0, 90.0);
    float angulo  = ((corrente_mA - 4.0) / 16.0) * 90.0;
    //angulo = constrain(angulo, 0.0, 90.0);
    client.print(corrente_mA, 3); // Entrada do sensor (mA)
    client.print(",");
    //enviar o ângulo real
    client.println(angulo, 3);
    //client.println(angulo - 52 + 1.5, 3); // Saída real (graus)
    ultimoEnvioCalibracaoTcp = millis();
  }

}

// === Processamento de comandos TCP ===
void processarComando(EthernetClient& client, String comando) {
  if (comando == "START1") {
    sentido1 = true;
    sentido2 = false;
    controleRemotoAtivo = true;
    modoEnsaioAtivo = false;
    client.println("OK - Motor sentido 1 ativado");
    
  } 
  else if (comando == "START2") {
    sentido1 = false;
    sentido2 = true;
    controleRemotoAtivo = true;
    modoEnsaioAtivo = false;
    client.println("OK - Motor sentido 2 ativado");
    
  }
   else if (comando == "STOP") {
    sentido1 = false;
    sentido2 = false;
    controleRemotoAtivo = true;
    modoEnsaioAtivo = false;
    client.println("OK - Motor parado");
    
  }
   else if (comando == "MANUAL") {
    controleRemotoAtivo = false;
    modoEnsaioAtivo = false;
    client.println("OK - Modo manual ativado");
    
  } 
  else if (comando.startsWith("SPEED")) {
    int equalPos = comando.indexOf('=');
    if (equalPos > 0) {
      float novaVelocidade = comando.substring(equalPos + 1).toFloat();
      if (novaVelocidade >= velocidadeMinima && novaVelocidade <= velocidadeMaxima) {
        setVelocidade(novaVelocidade);
        client.print("OK - Velocidade: ");
        client.print(novaVelocidade, 3);
        client.println(" graus/min");
      } 
      else {
        client.print("ERRO - Velocidade deve estar entre ");
        client.print(velocidadeMinima, 1);
        client.print(" e ");
        client.print(velocidadeMaxima, 1);
        client.println(" graus/min");
      }
    } 
    else {
      client.print("Velocidade atual: ");
      client.print(velocidadeDesejada, 3);
      client.println(" graus/min");
    }
    
  }
   else if (comando == "SPEED+") {
    incrementarVelocidade(1.0);
    client.print("OK - Velocidade: ");
    client.print(velocidadeDesejada, 3);
    client.println(" graus/min");
    
  }
   else if (comando == "SPEED-") {
    decrementarVelocidade(1.0);
    client.print("OK - Velocidade: ");
    client.print(velocidadeDesejada, 3);
    client.println(" graus/min");
    
  } 
  else if (comando == "STATUS") {
    client.print("Motor: ");
    client.print(motorAtivo ? "ATIVO" : "PARADO");
    client.print(" | Velocidade: ");
    client.print(velocidadeDesejada, 3);
    client.print(" graus/min | Emergencia: ");
    client.print(estadoEmergencia ? "ACIONADA" : "NORMAL");
    client.print(" | FC1: ");
    client.print(f1 ? "ATIVO" : "LIVRE");
    client.print(" | FC2: ");
    client.print(f2 ? "ATIVO" : "LIVRE");
    client.print(" | Controle: ");
    client.println(controleRemotoAtivo ? "REMOTO" : "MANUAL");
    
  } 
  else if (comando.startsWith("ENSAIO")){
    int pos = comando.indexOf('=');
    if (pos > 0){
       float vel = comando.substring(pos + 1).toFloat();
       if (vel >= velocidadeMinima && vel <= velocidadeMaxima){
          totalAngulos = 0;
          for (int i = 0; i < MAX_ANGULOS; i++) {
            angulosEnsaio[i] = 0.0;
            tempoEnsaio[i] = 0;
          }
          setVelocidade(vel);
          modoEnsaioAtivo = true;
          sentido1 = false;
          sentido2 = false;
          controleRemotoAtivo = false;
          client.print("OK - Ensaio iniciado a ");
          client.print(vel, 2);
          client.println(" graus/min (sentido 2)");
          client.println("tempo_ms,angulo_graus,mA");
       }
       else{
          client.print("ERRO - Velocidade deve estar entre ");
          client.print(velocidadeMinima, 1);
          client.print(" e ");
          client.print(velocidadeMaxima, 1);
          client.println(" graus/min");
       } 
    }
  }
  else if(comando == "RESULTADO"){
    if (totalAngulos == 0) {
      client.println("Nenhum dado registrado.");
    }
    else {
      client.println("tempo_ms,angulo_graus");
      for (int i = 0; i < totalAngulos; i++){
        client.print(tempoEnsaio[i]);
        client.print(",");
        client.println(angulosEnsaio[i], 2);
      }
    }
  }
  else if (comando == "REPOSICIONAR") {
    if (!estadoEmergencia && f1) { // FC1 está pressionado (ativo)
      modoReposicionamentoAtivo = true;
      client.println("REPOSICIONANDO");
    } 
    else {
      client.println("JA_POSICIONADA");
    }
  }
  else if (comando == "CALIBRACAO_START"){
    modoCalibracao = true;
    controleRemotoAtivo = false;
    modoEnsaioAtivo = false;
    client.println("CALIBRACAO_INICIADA");
    client.println("entrada_sensor,saida_real"); // Header CSV
  }
  else if (comando == "CALIBRACAO_STOP"){
    modoCalibracao = false;
    client.println("CALIBRACAO_FINALIZADA");
  }
  else if (comando == "CALIBRACAO_STATUS"){
    client.print("Modo calibração: ");
    client.println(modoCalibracao ? "ATIVO" : "INATIVO");
  }
  else if (comando == "MOTOR_INFO") {
    client.print("STR8 - Microstepping: ");
    client.print(MICROSTEPPING, 0);
    client.print(" | Redução: 1:");
    client.print(REDUCAO_MECANICA, 0);
    client.print(" | Graus/passo: ");
    client.print(GRAUS_POR_PASSO, 6);
    client.print(" | Timer: ");
    client.print(timerValue);
    client.print(" | Range: ");
    client.print(velocidadeMinima, 1);
    client.print("-");
    client.print(velocidadeMaxima, 1);
    client.println(" graus/min");
    
  } 
  else {
    client.println("ERRO - Comando não reconhecido");
    client.println("Comandos: START1, START2, STOP, MANUAL");
    client.println("SPEED=X.X, SPEED+, SPEED-, STATUS, MOTOR_INFO, ENSAIO=X.X, RESULTADO");
  }
}

// === Comunicação Ethernet ===
void lerComandoEthernet() {
    if (!ethernetConectado) return;

  // Aceita nova conexão se atual estiver desconectada
  if (!client || !client.connected()) {
    client = server.available();
    if (client) {
      client.setTimeout(50);  // Pequeno timeout só para segurança
      Serial.println("✅ Cliente conectado");
    }
  }

  static String buffer = "";

  // Lê comando linha a linha, sem travar o loop
  if (client && client.connected() && client.available()) {
    char c = client.read();
    if (c == '\n' || c == '\r') {
      buffer.trim();
      if (buffer.length() > 0) {
        processarComando(client, buffer);  // executa o comando
        buffer = "";
      }
    } else {
      buffer += c;
    }
  }
}



// === Leitura e debug ===
void printLeitura() {

  if (modoCalibracao) {
    float corrente_mA = adc.read4to20mA(channel);
    float angulo = ((corrente_mA - 12.0) / 8.0) * 90.0;
    angulo = constrain(angulo, -90.0, 90.0);
    
    Serial.print("📊 CAL: ");
    Serial.print(corrente_mA, 2);
    Serial.print(" mA -> ");
    //printar o ângulo real
    Serial.print(angulo, 2);
    //Serial.print(angulo - 52 + 1.5, 2);
    Serial.println("°");
    return; // Sai da função para não imprimir o debug completo
  }
  float corrente_mA = adc.read4to20mA(channel);
  //float angulo = ((corrente_mA - 12.0) / 8.0) * 90.0;
  //angulo = constrain(angulo, -90.0, 90.0);
   float angulo  = ((corrente_mA - 4.0) / 16.0) * 90.0;
   //angulo = constrain(angulo, 0.0, 90.0);
  float anguloSuave = aplicarFiltroExponencial(angulo);

  if (modoEnsaioAtivo && totalAngulos < MAX_ANGULOS) {
    
    client.print(millis());           // tempo,ângulo
    client.print(",");
    //enviar o ângulo real
    client.print(anguloSuave);
    //client.print(anguloSuave - 52);
    client.print(",");
    client.println(corrente_mA);
    angulosEnsaio[totalAngulos] = anguloSuave;
    tempoEnsaio[totalAngulos] = millis();
    totalAngulos++;
  }
  
  Serial.print("Corrente: ");
  Serial.print(corrente_mA, 2);
  Serial.print(" mA | Ângulo: ");
  Serial.print(angulo - 52 + 1.5, 2);
  Serial.print("° | Vel: ");
  Serial.print(velocidadeDesejada, 3);
  Serial.print(" graus/min | Motor: ");
  Serial.print(motorAtivo ? "ATIVO" : "PARADO");
  Serial.print(" | FC1: ");
  Serial.print(f1 ? "LIVRE" : "FECHADO");
  Serial.print(" | FC2: ");
  Serial.print(f2 ? "LIVRE" : "FECHADO");
  Serial.print(" | Controle: ");
  Serial.print(controleRemotoAtivo ? "REMOTO" : "MANUAL");
  Serial.print(" | ENSAIO: ");
  Serial.print(modoEnsaioAtivo ? "ENSAIO" : "NORMAL");
  Serial.println();
}

// === ISR Timer1 - Geração de pulsos de step ===
ISR(TIMER1_COMPA_vect) {
  if (motorAtivo) {
    stepState = !stepState;
    digitalWrite(stepPin, stepState);
  } else {
    digitalWrite(stepPin, LOW);
    stepState = false;
  }
}

// === Setup ===
void setup() {
  Serial.begin(115200);
  delay(2000);
  Serial.println("🚀 Sistema NEMA + STR8 - Controle de Velocidade");
  
  // Inicializa ADC
  adc.begin();
  adc.setGain(GAIN_TWO);
  
  // Configura pinos
  pinMode(stepPin, OUTPUT);
  pinMode(dirPin, OUTPUT);
  pinMode(BOTAOVERDE, INPUT_PULLUP);
  pinMode(BOTAOVERMELHO, INPUT_PULLUP);
  pinMode(emergencia, INPUT_PULLUP);
  pinMode(sensor, INPUT);
  pinMode(fc1, INPUT_PULLUP);
  pinMode(fc2, INPUT_PULLUP);
  
  // Mostra configuração do motor
  mostrarInfoMotor();
  
  // Configura Ethernet
  diagnosticarEthernet();
  if (ethernetConectado) {
    server.begin();
    Serial.print("✅ Servidor TCP na porta 5000 - IP: ");
    Serial.println(Ethernet.localIP());
  }


  // Configura Timer1
  setupTimer1();
  
  Serial.println("✅ Sistema inicializado!");
  Serial.println("=======================");
  Serial.println("Comandos TCP:");
  Serial.println("• SPEED=X.X - Define velocidade (graus/min)");
  Serial.println("• SPEED+/- - Incrementa/decrementa");
  Serial.println("• START1/START2 - Inicia movimento");
  Serial.println("• STOP - Para motor");
  Serial.println("• STATUS - Status completo");
  Serial.println("=======================");
}

// === Loop principal ===
void loop() {
  lerComandoEthernet();
  enviarDadosCalibracaoTcp();
  controleMotor();
  
  if (millis() - ultimaLeitura >= intervaloLeitura) {
    printLeitura();
    ultimaLeitura = millis();
  }
}
