/* USER CODE BEGIN Header */
/**
  ******************************************************************************
  * @file           : main.c
  * @brief          : Main program body
  ******************************************************************************
  * @attention
  *
  * Copyright (c) 2026 STMicroelectronics.
  * All rights reserved.
  *
  * This software is licensed under terms that can be found in the LICENSE file
  * in the root directory of this software component.
  * If no LICENSE file comes with the software, it is provided AS-IS.
  *
  ******************************************************************************
  */
/* USER CODE END Header */

/* Includes ------------------------------------------------------------------*/
#include "main.h"
#include "usb_device.h"

/* Private includes ----------------------------------------------------------*/
/* USER CODE BEGIN Includes */
#include "usbd_cdc_if.h"
#include <stdbool.h>
#include <stdint.h>
/* USER CODE END Includes */

/* Private typedef -----------------------------------------------------------*/
/* USER CODE BEGIN PTD */
/* USER CODE END PTD */

/* Private define ------------------------------------------------------------*/
/* USER CODE BEGIN PD */

#define LER_BOTAO HAL_GPIO_ReadPin(BTN_GPIO_Port, BTN_Pin)

#define NUM_AMOSTRAS 10
#define PROP_CONSTANTE 12.41
#define INTERVALO_ENVIO 1000

/* Protocolo proprietário */
#define SOF 0xAA
#define TIPO_TEMPERATURA 0x01
#define EOF_BYTE 0x55

#define TAMANHO_PACOTE 6

/* USER CODE END PD */

/* Private macro -------------------------------------------------------------*/
/* USER CODE BEGIN PM */
/* USER CODE END PM */

/* Private variables ---------------------------------------------------------*/
ADC_HandleTypeDef hadc1;

/* USER CODE BEGIN PV */
/* USER CODE END PV */

/* Private function prototypes -----------------------------------------------*/
void SystemClock_Config(void);
static void MX_GPIO_Init(void);
static void MX_ADC1_Init(void);

/* USER CODE BEGIN PFP */

float media(float nums[], uint32_t tamanho);

uint8_t CRC8(const uint8_t *dados, uint8_t tamanho);

void EnviarTemperatura(float temperatura);

/* USER CODE END PFP */

/* Private user code ---------------------------------------------------------*/
/* USER CODE BEGIN 0 */
/* USER CODE END 0 */

/**
  * @brief  The application entry point.
  * @retval int
  */
int main(void)
{
  /* USER CODE BEGIN 1 */
  /* USER CODE END 1 */

  /* MCU Configuration--------------------------------------------------------*/

  /* Reset of all peripherals, Initializes the Flash interface and the Systick. */
  HAL_Init();

  /* USER CODE BEGIN Init */
  /* USER CODE END Init */

  /* Configure the system clock */
  SystemClock_Config();

  /* USER CODE BEGIN SysInit */
  /* USER CODE END SysInit */

  /* Initialize all configured peripherals */
  MX_GPIO_Init();
  MX_ADC1_Init();
  MX_USB_DEVICE_Init();

  /* USER CODE BEGIN 2 */

  HAL_ADC_Start(&hadc1);

  uint8_t amostras_count = 0;
  float amostras[NUM_AMOSTRAS];
  uint8_t nova_amostra_index = 0;

  uint32_t ultimo_envio = 0;
  uint32_t tempo_atual = 0;

  bool filtrar = false;

  /* USER CODE END 2 */

  /* Infinite loop */
  /* USER CODE BEGIN WHILE */

  while (1)
  {
    if (LER_BOTAO == GPIO_PIN_RESET)
    {
      HAL_Delay(50);

      if (LER_BOTAO == GPIO_PIN_RESET)
      {
        filtrar = !filtrar;

        while (LER_BOTAO == GPIO_PIN_RESET)
        {
        }
      }
    }

    tempo_atual = HAL_GetTick();

    HAL_ADC_PollForConversion(&hadc1, HAL_MAX_DELAY);

    uint32_t adc = HAL_ADC_GetValue(&hadc1);

    float temperatura = (float)adc / PROP_CONSTANTE;

    amostras[nova_amostra_index] = temperatura;

    if (amostras_count < NUM_AMOSTRAS)
    {
      amostras_count++;
    }

    nova_amostra_index++;

    if (nova_amostra_index == NUM_AMOSTRAS)
    {
      nova_amostra_index = 0;
    }

    if ((tempo_atual - ultimo_envio) >= INTERVALO_ENVIO)
    {
      ultimo_envio = tempo_atual;

      if (amostras_count == NUM_AMOSTRAS && filtrar)
      {
        temperatura = media(amostras, NUM_AMOSTRAS);
      }

      if (!filtrar || amostras_count == NUM_AMOSTRAS)
      {
        EnviarTemperatura(temperatura);
      }
    }

    /* USER CODE END WHILE */

    /* USER CODE BEGIN 3 */
  }

  /* USER CODE END 3 */
}

/**
  * @brief System Clock Configuration
  * @retval None
  */
void SystemClock_Config(void)
{
  RCC_OscInitTypeDef RCC_OscInitStruct = {0};
  RCC_ClkInitTypeDef RCC_ClkInitStruct = {0};
  RCC_PeriphCLKInitTypeDef PeriphClkInit = {0};

  /** Initializes the RCC Oscillators according to the specified parameters
  * in the RCC_OscInitTypeDef structure.
  */
  RCC_OscInitStruct.OscillatorType = RCC_OSCILLATORTYPE_HSE;
  RCC_OscInitStruct.HSEState = RCC_HSE_ON;
  RCC_OscInitStruct.HSEPredivValue = RCC_HSE_PREDIV_DIV1;
  RCC_OscInitStruct.HSIState = RCC_HSI_ON;
  RCC_OscInitStruct.PLL.PLLState = RCC_PLL_ON;
  RCC_OscInitStruct.PLL.PLLSource = RCC_PLLSOURCE_HSE;
  RCC_OscInitStruct.PLL.PLLMUL = RCC_PLL_MUL6;

  if (HAL_RCC_OscConfig(&RCC_OscInitStruct) != HAL_OK)
  {
    Error_Handler();
  }

  /** Initializes the CPU, AHB and APB buses clocks
  */
  RCC_ClkInitStruct.ClockType = RCC_CLOCKTYPE_HCLK |
                                RCC_CLOCKTYPE_SYSCLK |
                                RCC_CLOCKTYPE_PCLK1 |
                                RCC_CLOCKTYPE_PCLK2;

  RCC_ClkInitStruct.SYSCLKSource = RCC_SYSCLKSOURCE_PLLCLK;
  RCC_ClkInitStruct.AHBCLKDivider = RCC_SYSCLK_DIV1;
  RCC_ClkInitStruct.APB1CLKDivider = RCC_HCLK_DIV2;
  RCC_ClkInitStruct.APB2CLKDivider = RCC_HCLK_DIV1;

  if (HAL_RCC_ClockConfig(&RCC_ClkInitStruct, FLASH_LATENCY_1) != HAL_OK)
  {
    Error_Handler();
  }

  PeriphClkInit.PeriphClockSelection = RCC_PERIPHCLK_ADC |
                                       RCC_PERIPHCLK_USB;

  PeriphClkInit.AdcClockSelection = RCC_ADCPCLK2_DIV4;
  PeriphClkInit.UsbClockSelection = RCC_USBCLKSOURCE_PLL;

  if (HAL_RCCEx_PeriphCLKConfig(&PeriphClkInit) != HAL_OK)
  {
    Error_Handler();
  }
}

/**
  * @brief ADC1 Initialization Function
  * @param None
  * @retval None
  */
static void MX_ADC1_Init(void)
{
  /* USER CODE BEGIN ADC1_Init 0 */
  /* USER CODE END ADC1_Init 0 */

  ADC_ChannelConfTypeDef sConfig = {0};

  /* USER CODE BEGIN ADC1_Init 1 */
  /* USER CODE END ADC1_Init 1 */

  /** Common config
  */
  hadc1.Instance = ADC1;
  hadc1.Init.ScanConvMode = ADC_SCAN_DISABLE;
  hadc1.Init.ContinuousConvMode = ENABLE;
  hadc1.Init.DiscontinuousConvMode = DISABLE;
  hadc1.Init.ExternalTrigConv = ADC_SOFTWARE_START;
  hadc1.Init.DataAlign = ADC_DATAALIGN_RIGHT;
  hadc1.Init.NbrOfConversion = 1;

  if (HAL_ADC_Init(&hadc1) != HAL_OK)
  {
    Error_Handler();
  }

  /** Configure Regular Channel
  */
  sConfig.Channel = ADC_CHANNEL_1;
  sConfig.Rank = ADC_REGULAR_RANK_1;
  sConfig.SamplingTime = ADC_SAMPLETIME_1CYCLE_5;

  if (HAL_ADC_ConfigChannel(&hadc1, &sConfig) != HAL_OK)
  {
    Error_Handler();
  }

  /* USER CODE BEGIN ADC1_Init 2 */
  /* USER CODE END ADC1_Init 2 */
}

/**
  * @brief GPIO Initialization Function
  * @param None
  * @retval None
  */
static void MX_GPIO_Init(void)
{
  GPIO_InitTypeDef GPIO_InitStruct = {0};

  /* GPIO Ports Clock Enable */
  __HAL_RCC_GPIOD_CLK_ENABLE();
  __HAL_RCC_GPIOA_CLK_ENABLE();

  /*Configure GPIO pin : BTN_Pin */
  GPIO_InitStruct.Pin = BTN_Pin;
  GPIO_InitStruct.Mode = GPIO_MODE_INPUT;
  GPIO_InitStruct.Pull = GPIO_PULLUP;

  HAL_GPIO_Init(BTN_GPIO_Port, &GPIO_InitStruct);
}

/* USER CODE BEGIN 4 */

/**
  * @brief Calcula o CRC-8.
  *
  * Algoritmo utilizado:
  * CRC-8/ATM
  *
  * Polinômio: 0x07
  * Valor inicial: 0x00
  *
  * O CRC é calculado sobre os dados do protocolo,
  * excluindo SOF, CRC e EOF.
  */
uint8_t CRC8(const uint8_t *dados, uint8_t tamanho)
{
  uint8_t crc = 0x00;

  for (uint8_t i = 0; i < tamanho; i++)
  {
    crc ^= dados[i];

    for (uint8_t bit = 0; bit < 8; bit++)
    {
      if (crc & 0x80)
      {
        crc = (crc << 1) ^ 0x07;
      }
      else
      {
        crc <<= 1;
      }
    }
  }

  return crc;
}


float media(float nums[], uint32_t tamanho)
{
  float soma = 0.0f;

  for (uint32_t i = 0; i < tamanho; i++)
  {
    soma += nums[i];
  }

  return soma / (float)tamanho;
}

/**
  * @brief
  *
  * Estrutura do pacote:
  *
  * Byte 0: SOF              = 0xAA
  * Byte 1: TIPO             = 0x01
  * Byte 2: TEMPERATURA_LSB
  * Byte 3: TEMPERATURA_MSB
  * Byte 4: CRC-8
  * Byte 5: EOF              = 0x55
  */
void EnviarTemperatura(float temperatura)
{
  uint8_t pacote[TAMANHO_PACOTE];

  /*
   *
   *
   *
   *
   *
   */
  int16_t temperatura_int =
      (int16_t)(temperatura * 100.0f);

  /* Início do pacote */
  pacote[0] = SOF;

  /* Identificação do tipo de dado */
  pacote[1] = TIPO_TEMPERATURA;

  /*
   * Temperatura em little-endian.
   *
   * Byte menos significativo.
   */
  pacote[2] =
      (uint8_t)(temperatura_int & 0xFF);

  /*
   * Byte mais significativo.
   */
  pacote[3] =
      (uint8_t)((temperatura_int >> 8) & 0xFF);

  /*
   * CRC calculado sobre:
   *
   * pacote[1] = TIPO
   * pacote[2] = TEMPERATURA_LSB
   * pacote[3] = TEMPERATURA_MSB
   *
   */
  pacote[4] =
      CRC8(&pacote[1], 3);

  /* Fim do pacote */
  pacote[5] = EOF_BYTE;

  /*
   * Transmite os 6 bytes.
   *
   */
  CDC_Transmit_FS(
      pacote,
      TAMANHO_PACOTE
  );
}

/* USER CODE END 4 */

/**
  * @brief  This function is executed in case of error occurrence.
  * @retval None
  */
void Error_Handler(void)
{
  /* USER CODE BEGIN Error_Handler_Debug */

  __disable_irq();

  while (1)
  {
  }

  /* USER CODE END Error_Handler_Debug */
}

#ifdef USE_FULL_ASSERT

/**
  * @brief  Reports the name of the source file and the source line number
  * @param  file: pointer to the source file name
  * @param  line: assert_param error line number
  * @retval None
  */
void assert_failed(uint8_t *file, uint32_t line)
{
  /* USER CODE BEGIN 6 */

  /*
     ex: printf("Wrong parameters value: file %s on line %d\r\n", file, line)
  */

  /* USER CODE END 6 */
}

#endif /* USE_FULL_ASSERT */

/************************ (C) COPYRIGHT STMicroelectronics *****END OF FILE****/
