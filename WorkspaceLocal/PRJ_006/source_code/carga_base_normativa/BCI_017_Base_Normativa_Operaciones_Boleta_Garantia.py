# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_017_Base_Normativa_Operaciones_Boleta_Garantia
# MAGIC **************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ## Información del Notebook

# COMMAND ----------

# MAGIC %md
# MAGIC ### Encabezado
# MAGIC ***************************************************************************************************************
# MAGIC * Nombre: BCI_017_Base_Normativa_Operaciones_Boleta_Garantia.ipynb
# MAGIC * Ruta: 
# MAGIC * Autor: BitSolucion Spa
# MAGIC * Ing. SW BCI: <alguien.de.ti@bci.cl> - Marcelo Lizama <marcelo.lizama@bci.cl> - Ivan Jara <ivan.jara@bci.cl>
# MAGIC * Fecha: 22/12/2025
# MAGIC * Descripción: Notebook encargado de generar la informacion de las operaciones boleta garantia
# MAGIC * Documentación: 
# MAGIC ***************************************************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Mantenciones
# MAGIC **************************************************************************
# MAGIC #### Mantención Nro: 
# MAGIC * Autor: <Nombre Autor> (<Empresa del Autor (Bci/Otra)>) - Ing. SW BCI: <Nombre Ing. SW BCI>
# MAGIC * Fecha: <dd/mm/yyyy> 
# MAGIC * Descripción: <Descripción de la mantención>      
# MAGIC ***************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tablas Entrada y Salida
# MAGIC **************************************************************************
# MAGIC #### Tablas Entrada: 
# MAGIC * tmp_d00_deuda_act_ctg
# MAGIC * tmp_modelo_com_gr
# MAGIC * tmp_modelo_indiv
# MAGIC ***************************************************************************
# MAGIC #### Tablas Salida: 
# MAGIC * base_archivos_normativos (Activo 44 Tabla 89 Manual CMF)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Captura de Variable

# COMMAND ----------

# MAGIC %md
# MAGIC ### Crear Widgets para Capturar de Variables

# COMMAND ----------

dbutils.widgets.removeAll()

dbutils.widgets.text("fechaProcesoW","","01 Fecha Proceso:")
dbutils.widgets.text("db_platinumW","","02 platinum DB:")
dbutils.widgets.text("db_plat_tempW","","03 platinum temp db:")
dbutils.widgets.text("db_RiesgoCredW","","04 slv_RiesgoCred_RiesgoCredPer DB:")
dbutils.widgets.text("db_slv_ParametricasW","","05 Parametricas DB:")

dbutils.widgets.text("db_location_plat_tempW","","06 platinum temp db:")

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignar Objeto a Lectura de Widgets y Variables

# COMMAND ----------

fechaProcesoX = dbutils.widgets.get("fechaProcesoW")
spark.conf.set("bci.fechaProcesoX", fechaProcesoX)

db_platinumX = dbutils.widgets.get("db_platinumW")
spark.conf.set("bci.db_platinumX", db_platinumX)

db_plat_tempX = dbutils.widgets.get("db_plat_tempW")
spark.conf.set("bci.db_plat_tempX", db_plat_tempX)

db_RiesgoCredX = dbutils.widgets.get("db_RiesgoCredW")
spark.conf.set("bci.db_RiesgoCredX", db_RiesgoCredX)

db_slv_ParametricasX = dbutils.widgets.get("db_slv_ParametricasW")
spark.conf.set("bci.db_slv_ParametricasX", db_slv_ParametricasX)


db_location_plat_tempX = dbutils.widgets.get("db_location_plat_tempW")
spark.conf.set("bci.db_location_plat_tempX", db_location_plat_tempX)


print("Parámetros")
print("fechaProcesoX: " + fechaProcesoX)
print("db_platinumX: " + db_platinumX)
print("db_plat_tempX: " + db_plat_tempX)
print("db_RiesgoCredX: " + db_RiesgoCredX)
print("db_slv_ParametricasX: " + db_slv_ParametricasX)
print("db_location_plat_tempX: " + db_location_plat_tempX)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Carga de funciones

# COMMAND ----------

# MAGIC %run "../../Funciones"

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validación de ingreso de parámetros

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Parametro Vacio

# COMMAND ----------

valida_param_vacio(fechaProcesoX,'fechaProcesoX')
valida_param_vacio(db_platinumX,'db_platinumX')
valida_param_vacio(db_plat_tempX,'db_plat_tempX')
valida_param_vacio(db_RiesgoCredX,'db_RiesgoCredX')
valida_param_vacio(db_slv_ParametricasX,'db_slv_ParametricasX')
valida_param_vacio(db_location_plat_tempX,'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Base de Datos

# COMMAND ----------

valida_bd(db_platinumX, 'db_platinumX')
valida_bd(db_plat_tempX, 'db_plat_tempX')
valida_bd(db_RiesgoCredX, 'db_RiesgoCredX')
valida_bd(db_slv_ParametricasX, 'db_slv_ParametricasX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Fecha Valida

# COMMAND ----------

valida_fecha_valida(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Fecha Futura

# COMMAND ----------

valida_fecha_futura(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Ruta 

# COMMAND ----------

valida_ruta(db_location_plat_tempX, 'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignacion Variables de fecha

# COMMAND ----------

ano = str(fechaProcesoX)[:4]
mes = str(fechaProcesoX)[4:][:2]
dia = str(fechaProcesoX)[6:][:2]
fechanormativo = str(ano+'-'+mes+'-'+dia)
fechacinta = str(dia+'-'+mes+'-'+ano)
anomes = str(ano+mes)
anomesdia = str(ano+mes+dia)
ano_mes_dia = str(ano+'-'+mes+'-01')

print("fecha_Formato1: " + ano)
print("fecha_Formato2: " + mes)
print("fecha_Formato3: " + dia)
print("fecha_Formato4: " + fechanormativo)
print("fecha_Formato5: " + fechacinta)
print("fecha_Formato6: " + anomes)
print("fecha_Formato7: " + anomesdia)
print("fecha_Formato8: " + ano_mes_dia)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Inicio de Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ### Creacion Tablas Temporales

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci17_basenorma_tmp_cli_duplicados

# COMMAND ----------

drop_cli_duplicados = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci17_basenorma_tmp_cli_duplicados"""

# COMMAND ----------

sql_safe(drop_cli_duplicados)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci17_basenorma_tmp_cli_duplicados", True)

# COMMAND ----------

create_cli_duplicados = """CREATE TABLE """ + db_plat_tempX + """.bci17_basenorma_tmp_cli_duplicados
(
  periodo_id                      INT, 				
  rut_cliente                     STRING, 				
  dv_cliente                      STRING, 							
  cod_num_operacion               STRING
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci17_basenorma_tmp_cli_duplicados' """

# COMMAND ----------

sql_safe(create_cli_duplicados)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci17_basenorma_tmp_cli_dupli_filtrado

# COMMAND ----------

drop_cli_dupli_filtrado = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci17_basenorma_tmp_cli_dupli_filtrado"""

# COMMAND ----------

sql_safe(drop_cli_dupli_filtrado)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci17_basenorma_tmp_cli_dupli_filtrado", True)

# COMMAND ----------

create_cli_dupli_filtrado = """CREATE TABLE """ + db_plat_tempX + """.bci17_basenorma_tmp_cli_dupli_filtrado
(
  periodo_id                      INT, 				
  rut_cliente                     STRING, 				
  dv_cliente                      STRING, 							
  cod_num_operacion               STRING
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci17_basenorma_tmp_cli_dupli_filtrado' """

# COMMAND ----------

sql_safe(create_cli_dupli_filtrado)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci17_basenorma_tmp_cli_not_problem

# COMMAND ----------

drop_cli_not_problem = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci17_basenorma_tmp_cli_not_problem"""

# COMMAND ----------

sql_safe(drop_cli_not_problem)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci17_basenorma_tmp_cli_not_problem", True)

# COMMAND ----------

create_cli_not_problem = """CREATE TABLE """ + db_plat_tempX + """.bci17_basenorma_tmp_cli_not_problem
(
  periodo_id                      INT, 				
  rut_cliente                     STRING, 				
  dv_cliente                      STRING, 				
  cod_ope_original                STRING, 			
  cod_num_operacion               STRING, 		
  cod_tip_cart                    STRING, 				
  cod_tipo_ope                    STRING, 				
  des_producto_ope                STRING, 			
  des_banca_ope                   STRING, 			
  cod_cartera_ope                 STRING,			
  mto_deuda_ope                   DECIMAL(30, 0),	   			
  num_dias_mora_ope               INT,		
  ind_cdet                      	STRING,					
  fec_ingreso_deteriodo_ope       STRING,
  cod_cctb                      	STRING, 					
  fl_ope_reneg                  	STRING
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci17_basenorma_tmp_cli_not_problem' """

# COMMAND ----------

sql_safe(create_cli_not_problem)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci17_basenorma_tmp_cli_with_problem

# COMMAND ----------

drop_cli_with_problem = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci17_basenorma_tmp_cli_with_problem"""

# COMMAND ----------

sql_safe(drop_cli_with_problem)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci17_basenorma_tmp_cli_with_problem", True)

# COMMAND ----------

create_cli_with_problem = """CREATE TABLE """ + db_plat_tempX + """.bci17_basenorma_tmp_cli_with_problem
(
  periodo_id                      INT, 				
  rut_cliente                     STRING, 				
  dv_cliente                      STRING, 				
  cod_ope_original                STRING, 			
  cod_num_operacion               STRING, 		
  cod_tip_cart                    STRING, 				
  cod_tipo_ope                    STRING, 				
  des_producto_ope                STRING, 			
  des_banca_ope                   STRING, 			
  cod_cartera_ope                 STRING,			
  mto_deuda_ope                   DECIMAL(30, 0),	   			
  num_dias_mora_ope               INT,		
  ind_cdet                      	STRING,					
  fec_ingreso_deteriodo_ope       STRING,
  cod_cctb                      	STRING, 					
  fl_ope_reneg                  	STRING
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci17_basenorma_tmp_cli_with_problem' """

# COMMAND ----------

sql_safe(create_cli_with_problem)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci17_basenorma_tmp_boleta_grtia_not_problem

# COMMAND ----------

drop_boleta_grtia_not_problem = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci17_basenorma_tmp_boleta_grtia_not_problem"""

# COMMAND ----------

sql_safe(drop_boleta_grtia_not_problem)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci17_basenorma_tmp_boleta_grtia_not_problem", True)

# COMMAND ----------

create_boleta_grtia_not_problem = """CREATE TABLE """ + db_plat_tempX + """.bci17_basenorma_tmp_boleta_grtia_not_problem
(
  periodo_id                      INT, 				
  rut_cliente                     STRING, 				
  dv_cliente                      STRING, 				
  cod_ope_original                STRING, 			
  cod_num_operacion               STRING, 		
  cod_tip_cart                    STRING, 				
  cod_tipo_ope                    STRING, 				
  des_producto_ope                STRING, 			
  des_banca_ope                   STRING, 			
  cod_cartera_ope                 STRING,			
  mto_deuda_ope                   DECIMAL(30, 0),	   			
  num_dias_mora_ope               INT,		
  ind_cdet                      	STRING,					
  fec_ingreso_deteriodo_ope       STRING,
  cod_cctb                      	STRING, 					
  fl_ope_reneg                  	STRING,
  tipo_activo               	    INT,
  nombre_activo             	    STRING,
  pi_cli                   	      STRING,
  pdi_cli                   	    DECIMAL(30, 6),
  pe_cli                   	      DECIMAL(30, 7),
  provision                   	  STRING,
  tipo_cli                   	    STRING,
  factor_expo                   	DECIMAL(30, 6),
  tipo_garantia                   STRING,
  monto_garantia                  STRING,
  pvtg                   	        DECIMAL(30, 6),
  pi_metodo_interno               DECIMAL(30, 6),
  pdi_metodo_interno              DECIMAL(30, 6),
  pe_metodo_interno               DECIMAL(30, 6),
  prov_metodo_interno             DECIMAL(30, 0),
  monto_avalado					          DECIMAL(30, 0)
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci17_basenorma_tmp_boleta_grtia_not_problem' """

# COMMAND ----------

sql_safe(create_boleta_grtia_not_problem)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci17_basenorma_tmp_boleta_grtia_not_problem_2

# COMMAND ----------

drop_boleta_grtia_not_problem_2 = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci17_basenorma_tmp_boleta_grtia_not_problem_2"""

# COMMAND ----------

sql_safe(drop_boleta_grtia_not_problem_2)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci17_basenorma_tmp_boleta_grtia_not_problem_2", True)

# COMMAND ----------

create_boleta_grtia_not_problem_2 = """CREATE TABLE """ + db_plat_tempX + """.bci17_basenorma_tmp_boleta_grtia_not_problem_2
(
  periodo_id                      INT, 				
  rut_cliente                     STRING, 				
  dv_cliente                      STRING, 				
  cod_ope_original                STRING, 			
  cod_num_operacion               STRING, 		
  cod_tip_cart                    STRING, 				
  cod_tipo_ope                    STRING, 				
  des_producto_ope                STRING, 			
  des_banca_ope                   STRING, 			
  cod_cartera_ope                 STRING,			
  mto_deuda_ope                   DECIMAL(30, 0),	   			
  num_dias_mora_ope               INT,		
  ind_cdet                      	STRING,					
  fec_ingreso_deteriodo_ope       STRING,
  cod_cctb                      	STRING, 					
  fl_ope_reneg                  	STRING,
  tipo_activo               	    INT,
  nombre_activo             	    STRING,
  pi_cli                   	      DECIMAL(30, 6),
  pdi_cli                   	    DECIMAL(30, 6),
  pe_cli                   	      DECIMAL(30, 7),
  provision                   	  DECIMAL(30, 0),
  tipo_cli                   	    STRING,
  exposicion                      DECIMAL(30, 0),
  factor_expo                   	DECIMAL(30, 6),
  tipo_garantia                   STRING,
  monto_garantia                  DECIMAL(30, 0),
  pvtg                   	        DECIMAL(30, 6),
  pi_metodo_interno               DECIMAL(30, 6),
  pdi_metodo_interno              DECIMAL(30, 6),
  pe_metodo_interno               DECIMAL(30, 6),
  prov_metodo_interno             DECIMAL(30, 0),
  monto_avalado					          DECIMAL(30, 0)
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci17_basenorma_tmp_boleta_grtia_not_problem_2' """

# COMMAND ----------

sql_safe(create_boleta_grtia_not_problem_2)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci17_basenorma_tmp_boleta_grtia_with_problem

# COMMAND ----------

drop_boleta_grtia_with_problem = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci17_basenorma_tmp_boleta_grtia_with_problem"""

# COMMAND ----------

sql_safe(drop_boleta_grtia_with_problem)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci17_basenorma_tmp_boleta_grtia_with_problem", True)

# COMMAND ----------

create_boleta_grtia_with_problem = """CREATE TABLE """ + db_plat_tempX + """.bci17_basenorma_tmp_boleta_grtia_with_problem
(
  periodo_id                      INT, 				
  rut_cliente                     INT, 				
  dv_cliente                      STRING, 				
  cod_ope_original                STRING, 			
  cod_num_operacion               STRING, 		
  cod_tip_cart                    STRING, 				
  cod_tipo_ope                    STRING, 				
  des_producto_ope                STRING, 			
  des_banca_ope                   STRING, 			
  cod_cartera_ope                 STRING,			
  mto_deuda_ope                   DECIMAL(30, 0),	   			
  num_dias_mora_ope               INT,		
  ind_cdet                      	STRING,					
  fec_ingreso_deteriodo_ope       STRING,
  cod_cctb                      	STRING, 					
  fl_ope_reneg                  	STRING,
  tipo_activo               	    INT,
  nombre_activo             	    STRING,
  pi_cli                   	      DECIMAL(12, 6),
  pdi_cli                   	    DECIMAL(12, 6),
  pe_cli                   	      DECIMAL(12, 6),
  provision                   	  DECIMAL(30, 0),
  tipo_cli                   	    STRING,
  exposicion                      DECIMAL(30, 0),
  factor_expo                   	DECIMAL(30, 6),
  tipo_garantia                   STRING,
  monto_garantia                  DECIMAL(30, 0),
  pct_ltv                	        DECIMAL(12, 6),
  pi_metodo_interno               DECIMAL(12, 6),
  pdi_metodo_interno              DECIMAL(12, 6),
  pe_metodo_interno               DECIMAL(12, 6),
  prov_metodo_interno             DECIMAL(30, 0),
  monto_avalado					          DECIMAL(30, 0)
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci17_basenorma_tmp_boleta_grtia_with_problem' """

# COMMAND ----------

sql_safe(create_boleta_grtia_with_problem)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci17_basenorma_tmp_boleta_grtia_with_problem_2

# COMMAND ----------

drop_boleta_grtia_with_problem_2 = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci17_basenorma_tmp_boleta_grtia_with_problem_2"""

# COMMAND ----------

sql_safe(drop_boleta_grtia_with_problem_2)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci17_basenorma_tmp_boleta_grtia_with_problem_2", True)

# COMMAND ----------

create_boleta_grtia_with_problem_2 = """CREATE TABLE """ + db_plat_tempX + """.bci17_basenorma_tmp_boleta_grtia_with_problem_2
(
  periodo_id                      INT, 				
  rut_cliente                     INT, 				
  dv_cliente                      STRING, 				
  cod_ope_original                STRING, 			
  cod_num_operacion               STRING, 		
  cod_tip_cart                    STRING, 				
  cod_tipo_ope                    STRING, 				
  des_producto_ope                STRING, 			
  des_banca_ope                   STRING, 			
  cod_cartera_ope                 STRING,			
  mto_deuda_ope                   DECIMAL(30, 0),	   			
  num_dias_mora_ope               INT,		
  ind_cdet                      	STRING,					
  fec_ingreso_deteriodo_ope       STRING,
  cod_cctb                      	STRING, 					
  fl_ope_reneg                  	STRING,
  tipo_activo               	    INT,
  nombre_activo             	    STRING,
  pi_cli                   	      DECIMAL(30, 6),
  pdi_cli                   	    DECIMAL(30, 6),
  pe_cli                   	      DECIMAL(30, 7),
  provision                   	  DECIMAL(30, 0),
  tipo_cli                   	    STRING,
  exposicion                      DECIMAL(30, 0),
  factor_expo                   	DECIMAL(30, 6),
  tipo_garantia                   STRING,
  monto_garantia                  DECIMAL(30, 0),
  pvtg                   	        DECIMAL(30, 6),
  pi_metodo_interno               DECIMAL(30, 6),
  pdi_metodo_interno              DECIMAL(30, 6),
  pe_metodo_interno               DECIMAL(30, 6),
  prov_metodo_interno             DECIMAL(30, 0),
  monto_avalado					          DECIMAL(30, 0)
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci17_basenorma_tmp_boleta_grtia_with_problem_2' """

# COMMAND ----------

sql_safe(create_boleta_grtia_with_problem_2)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci17_tmp_boleta_grtia_with_problem_resp

# COMMAND ----------

drop_boleta_grtia_with_problem_resp = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci17_tmp_boleta_grtia_with_problem_resp"""

# COMMAND ----------

sql_safe(drop_boleta_grtia_with_problem_resp)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci17_tmp_boleta_grtia_with_problem_resp ", True)

# COMMAND ----------

create_boleta_grtia_with_problem_resp = """CREATE TABLE """ + db_plat_tempX + """.bci17_tmp_boleta_grtia_with_problem_resp
(
  periodo_id                      INT, 				
  rut_cliente                     INT, 				
  dv_cliente                      STRING, 				
  cod_ope_original                STRING, 			
  cod_num_operacion               STRING, 		
  cod_tip_cart                    STRING, 				
  cod_tipo_ope                    STRING, 				
  des_producto_ope                STRING, 			
  des_banca_ope                   STRING, 			
  cod_cartera_ope                 STRING,			
  mto_deuda_ope                   DECIMAL(30, 0),	   			
  num_dias_mora_ope               INT,		
  ind_cdet                      	STRING,					
  fec_ingreso_deteriodo_ope       STRING,
  cod_cctb                      	STRING, 					
  fl_ope_reneg                  	STRING,
  tipo_activo               	    INT,
  nombre_activo             	    STRING,
  pi_cli                   	      DECIMAL(12, 6),
  pdi_cli                   	    DECIMAL(12, 6),
  pe_cli                   	      DECIMAL(12, 6),
  provision                   	  DECIMAL(30, 0),
  tipo_cli                   	    STRING,
  exposicion                      DECIMAL(30, 0),
  factor_expo                   	DECIMAL(30, 6),
  tipo_garantia                   STRING,
  monto_garantia                  DECIMAL(30, 0),
  pct_ltv                	        DECIMAL(12, 6),
  pi_metodo_interno               DECIMAL(12, 6),
  pdi_metodo_interno              DECIMAL(12, 6),
  pe_metodo_interno               DECIMAL(12, 6),
  prov_metodo_interno             DECIMAL(30, 0),
  monto_avalado					          DECIMAL(30, 0)
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci17_tmp_boleta_grtia_with_problem_resp' """

# COMMAND ----------

sql_safe(create_boleta_grtia_with_problem_resp)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Desarrollo Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 1: Obtencion Ultimo dia Habil 

# COMMAND ----------

ult_dia_mes = ultimo_dia_habil(db_slv_ParametricasX, anomes)

ult_dia = ult_dia_mes.toPandas()
fec_proceso = ult_dia.iloc[0]["periodo_id"]

ano_proceso = str(fec_proceso)[:4]
mes_proceso = str(fec_proceso)[4:][:2]
dia_proceso = str(fec_proceso)[6:][:2]
date_proceso = str(ano_proceso+'-'+mes_proceso+'-'+dia_proceso)
date_proceso_2 = str(ano_proceso+mes_proceso+dia_proceso)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 2: Obtencion Univ Clientes Duplicados

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de los clientes duplicados desde tmp_d00_deuda_act_ctg y TBU
# MAGIC
# MAGIC **Créditos Contingentes Transacciones relacionadas con eventos contingentes (8315)**
# MAGIC - **831500100**:	Transacciones relacionadas con eventos contingentes en moneda chilena
# MAGIC - **831500200**:	Transacciones relacionadas con eventos contingentes en moneda extranjera
# MAGIC
# MAGIC **Tabla de salida**: bci17_basenorma_tmp_cli_duplicados (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente				
# MAGIC - **cod_num_operacion**: Número Operación 		

# COMMAND ----------

diaHabil = spark.sql("""select max(fecha_informada) as periodo_id  FROM  """+ db_slv_ParametricasX +""".TBU  
where substring(fecha_informada, 1, 6) = """+ anomes +"""  """)

ultimoDiaHabil_tbu = diaHabil.toPandas()
dia_tbu = ultimoDiaHabil_tbu.iloc[0]["periodo_id"]
 
print(dia_tbu)

# COMMAND ----------

vista_cli_duplicados = """ insert into """ + db_plat_tempX + """.bci17_basenorma_tmp_cli_duplicados
  select  a.periodo_id, 				
          a.rut_cliente, 				
          a.dv_cliente, 							
          a.cod_num_operacion
  from """ + db_plat_tempX + """.tmp_d00_deuda_act_ctg a
  inner join """ + db_slv_ParametricasX + """.TBU b
      on trim(a.cod_cctb) = trim(b.cta_ctable)
      and a.periodo_id = substring(b.fecha_informada, 1, 6)
  where a.periodo_id = '""" + anomes + """'
    and substring(trim(b.cta_cmf_mc),1,4) = '8315'
    and trim(a.cod_cartera_ope) = 'CTG'
    and trim(a.fl_ope_reneg) <> 'C'
    and b.fecha_informada = """ + str(dia_tbu) + """
"""

# COMMAND ----------

sql_safe(vista_cli_duplicados)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 3: Obtencion Univ Clientes Sin Problemas

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para filtrar a los clientes duplicados desde bci17_basenorma_tmp_cli_duplicados y tmp_modelo_indiv
# MAGIC
# MAGIC **Tabla de salida**: bci17_basenorma_tmp_cli_dupli_filtrado (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente				
# MAGIC - **cod_num_operacion**: Número Operación 		

# COMMAND ----------

vista_cli_dupli_filtrado = """ insert into """ + db_plat_tempX + """.bci17_basenorma_tmp_cli_dupli_filtrado
  select  a.periodo_id 				
      ,a.rut_cliente
      ,a.dv_cliente 							
      ,a.cod_num_operacion
  from """ + db_plat_tempX + """.bci17_basenorma_tmp_cli_duplicados a
  inner join """ + db_plat_tempX + """.tmp_modelo_indiv b
      on a.periodo_id = b.periodo_id
      and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
      and b.mnt_provision <> 0
  group by a.periodo_id, a.rut_cliente, a.dv_cliente, a.cod_num_operacion
  having count(*) > 1
"""

# COMMAND ----------

sql_safe(vista_cli_dupli_filtrado)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 4: Obtencion Clientes Sin Problemas

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de los clientes sin problemas de duplicidad desde tmp_d00_deuda_act_ctg y TBU
# MAGIC
# MAGIC **Créditos Contingentes Transacciones relacionadas con eventos contingentes (8315)**
# MAGIC - **831500100**:	Transacciones relacionadas con eventos contingentes en moneda chilena
# MAGIC - **831500200**:	Transacciones relacionadas con eventos contingentes en moneda extranjera
# MAGIC
# MAGIC **Tabla de salida**: bci17_basenorma_tmp_cli_not_problem (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada	

# COMMAND ----------

vista_cli_not_problem = """ insert into """ + db_plat_tempX + """.bci17_basenorma_tmp_cli_not_problem
select a.periodo_id
      ,a.rut_cliente		
      ,a.dv_cliente 				
      ,a.cod_ope_original
      ,a.cod_num_operacion
      ,a.cod_tip_cart			
      ,a.cod_tipo_ope
      ,a.des_producto_ope
      ,a.des_banca_ope		
      ,a.cod_cartera_ope
      ,a.mto_deuda_ope	   			
      ,a.num_dias_mora_ope
      ,a.ind_cdet		
      ,a.fec_ingreso_deteriodo_ope
      ,a.cod_cctb
      ,a.fl_ope_reneg
from """ + db_plat_tempX + """.tmp_d00_deuda_act_ctg a
inner join """ + db_slv_ParametricasX + """.TBU b
      on trim(a.cod_cctb) = trim(b.cta_ctable)
      and a.periodo_id = substring(b.fecha_informada, 1, 6)
where a.periodo_id = '""" + anomes + """'
    and substring(trim(b.cta_cmf_mc), 1,4) = '8315'
    and trim(a.cod_cartera_ope) = 'CTG'
    and trim(a.fl_ope_reneg) <> 'C'
    and b.fecha_informada = """ + str(dia_tbu) + """
    and trim(a.cod_num_operacion) not in (select distinct trim(cod_num_operacion) as cod_num_operacion from """ + db_plat_tempX + """.bci17_basenorma_tmp_cli_dupli_filtrado)
"""

# COMMAND ----------

sql_safe(vista_cli_not_problem)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 5: Obtencion Clientes Con Problemas

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de los clientes con problemas de duplicidad desde tmp_d00_deuda_act_ctg y TBU
# MAGIC
# MAGIC **Créditos Contingentes Transacciones relacionadas con eventos contingentes (8315)**
# MAGIC - **831500100**:	Transacciones relacionadas con eventos contingentes en moneda chilena
# MAGIC - **831500200**:	Transacciones relacionadas con eventos contingentes en moneda extranjera
# MAGIC
# MAGIC **Tabla de salida**: bci17_basenorma_tmp_cli_with_problem (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada	

# COMMAND ----------

vista_cli_with_problem = """ insert into """ + db_plat_tempX + """.bci17_basenorma_tmp_cli_with_problem
select a.periodo_id
      ,a.rut_cliente		
      ,a.dv_cliente 				
      ,a.cod_ope_original
      ,a.cod_num_operacion
      ,a.cod_tip_cart			
      ,a.cod_tipo_ope
      ,a.des_producto_ope
      ,a.des_banca_ope		
      ,a.cod_cartera_ope
      ,a.mto_deuda_ope	   			
      ,a.num_dias_mora_ope
      ,a.ind_cdet		
      ,a.fec_ingreso_deteriodo_ope
      ,a.cod_cctb
      ,a.fl_ope_reneg
from """ + db_plat_tempX + """.tmp_d00_deuda_act_ctg a
inner  join """ + db_slv_ParametricasX + """.TBU b
    on trim(a.cod_cctb) = trim(b.cta_ctable)
    and a.periodo_id = substring(b.fecha_informada, 1, 6)
where a.periodo_id = '""" + anomes + """'
    and substring(trim(b.cta_cmf_mc), 1,4) = '8315'
    and trim(a.cod_cartera_ope) = 'CTG'
    and trim(a.fl_ope_reneg) <> 'C'
    and b.fecha_informada = """ + str(dia_tbu) + """
    and trim(a.cod_num_operacion) in (select distinct trim(cod_num_operacion) as cod_num_operacion from """ + db_plat_tempX + """.bci17_basenorma_tmp_cli_dupli_filtrado)
"""

# COMMAND ----------

sql_safe(vista_cli_with_problem)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 6: Obtencion Univ Operaciones Boletas Garantias Sin Problemas

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones boletas garantia sin duplicidad desde bci17_basenorma_tmp_cli_not_problem y tmp_modelo_com_gr
# MAGIC
# MAGIC **Tabla de salida**: bci17_basenorma_tmp_boleta_grtia_not_problem (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **tipo_activo**: Identificador del tipo activos
# MAGIC - **nombre_activo**: Nombre del tipo activo
# MAGIC - **pi_cli**: Porcentaje probabilidad de incumplimiento cliente
# MAGIC - **pdi_cli**: Porcentaje de perdida dada por el incumplimiento cliente
# MAGIC - **pe_cli**: Porcentaje de perdida esperada cliente
# MAGIC - **provision**: Monto provision cliente
# MAGIC - **tipo_cli**: Codigo tipo de cliente
# MAGIC - **factor_expo**: Porcentaje factor exposicion
# MAGIC - **tipo_garantia**: Codigo del tipo de garantia cliente
# MAGIC - **monto_garantia**: Monto de garantia cliente
# MAGIC - **pvtg**: Porcentaje PVTG
# MAGIC - **pi_metodo_interno**: Porcentaje probabilidad de incumplimiento interno
# MAGIC - **pdi_metodo_interno**: Porcentaje de perdida dada por el incumplimiento interno
# MAGIC - **pe_metodo_interno**: Porcentaje de perdida esperada interno
# MAGIC - **prov_metodo_interno**: Monto provision metodo interno
# MAGIC - **monto_avalado**: Monto avalado del cliente

# COMMAND ----------

vista_boleta_grtia_not_problem = """ insert into """ + db_plat_tempX + """.bci17_basenorma_tmp_boleta_grtia_not_problem
select  a.periodo_id				
      ,a.rut_cliente
      ,a.dv_cliente
      ,a.cod_ope_original
      ,a.cod_num_operacion
      ,a.cod_tip_cart			
      ,a.cod_tipo_ope
      ,a.des_producto_ope
      ,a.des_banca_ope		
      ,a.cod_cartera_ope
      ,a.mto_deuda_ope
      ,a.num_dias_mora_ope
      ,a.ind_cdet
      ,a.fec_ingreso_deteriodo_ope
      ,a.cod_cctb
      ,a.fl_ope_reneg
      ,44                                        as tipo_activo
      ,'BOLETAS GARANTIAS OK'                    as nombre_activo
      ,b.pct_pi                                  as pi_cli
      ,coalesce(b.pct_pdi, 0)				             as pdi_cli
      ,coalesce(b.pct_pe, 0)				             as pe_cli
      ,b.mnt_prov_oficial                        as provision
      ,case when b.periodo_id is null then 'SIN'
        else 'GR'  end                           as tipo_cli
      ,0.5                                       as factor_expo
      ,b.des_tipo_gtia                           as tipo_garantia
      ,b.mnt_garantia                            as monto_garantia
      ,coalesce(b.pct_ltv, 0)                    as pct_ltv
      ,coalesce(b.pct_pi_metodo_interno, 0)      as pi_metodo_interno
      ,coalesce(b.pct_pdi_metodo_interno, 0)     as pdi_metodo_interno
      ,coalesce(b.pct_pe_metodo_interno, 0)      as pe_metodo_interno
      ,coalesce(b.mnt_prov_int, 0.0)             as prov_metodo_interno
      ,coalesce(b.mnt_avalado, 0.0)              as monto_avalado	
from """ + db_plat_tempX + """.bci17_basenorma_tmp_cli_not_problem a
left join """ + db_plat_tempX + """.tmp_modelo_com_gr b
    on a.periodo_id = b.periodo_id
    and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
    and trim(a.cod_cartera_ope) = trim(b.cod_stp_cart)
where a.periodo_id = """+anomes+"""
"""

# COMMAND ----------

sql_safe(vista_boleta_grtia_not_problem)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 7: Obtencion Operaciones Boletas Garantia Sin Problemas y Nulos

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones boleta garantia sin duplicidad y nulos desde bci17_basenorma_tmp_boleta_grtia_not_problem y tmp_modelo_indiv
# MAGIC
# MAGIC **Tabla de salida**: bci17_basenorma_tmp_boleta_grtia_not_problem_2 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **tipo_activo**: Identificador del tipo activos
# MAGIC - **nombre_activo**: Nombre del tipo activo
# MAGIC - **pi_cli**: Porcentaje probabilidad de incumplimiento cliente
# MAGIC - **pdi_cli**: Porcentaje de perdida dada por el incumplimiento cliente
# MAGIC - **pe_cli**: Porcentaje de perdida esperada cliente
# MAGIC - **provision**: Monto provision cliente
# MAGIC - **tipo_cli**: Codigo tipo de cliente
# MAGIC - **exposicion**: Monto de exposicion
# MAGIC - **factor_expo**: Porcentaje factor exposicion
# MAGIC - **tipo_garantia**: Codigo del tipo de garantia cliente
# MAGIC - **monto_garantia**: Monto de garantia cliente
# MAGIC - **pvtg**: Porcentaje PVTG
# MAGIC - **pi_metodo_interno**: Porcentaje probabilidad de incumplimiento interno
# MAGIC - **pdi_metodo_interno**: Porcentaje de perdida dada por el incumplimiento interno
# MAGIC - **pe_metodo_interno**: Porcentaje de perdida esperada interno
# MAGIC - **prov_metodo_interno**: Monto provision metodo interno
# MAGIC - **monto_avalado**: Monto avalado del cliente

# COMMAND ----------

vista_boleta_grtia_not_problem_2 = """ insert into """ + db_plat_tempX + """.bci17_basenorma_tmp_boleta_grtia_not_problem_2
  select  a.periodo_id
      ,a.rut_cliente
      ,a.dv_cliente 				
      ,a.cod_ope_original
      ,a.cod_num_operacion
      ,a.cod_tip_cart			
      ,a.cod_tipo_ope
      ,a.des_producto_ope
      ,a.des_banca_ope		
      ,a.cod_cartera_ope
      ,a.mto_deuda_ope	   			
      ,a.num_dias_mora_ope
      ,a.ind_cdet		
      ,a.fec_ingreso_deteriodo_ope
      ,a.cod_cctb
      ,a.fl_ope_reneg
      ,a.tipo_activo
      ,a.nombre_activo
      ,coalesce(a.pi_cli, b.pct_pi)				              as pi_cli
      ,a.pdi_cli
      ,a.pe_cli
      ,coalesce(a.provision, b.mnt_provision)		        as provision
      ,case when trim(a.tipo_cli) = 'SIN' then b.cod_calificacion
            else 'GR'  end											                          as tipo_cli
      ,round((case when trim(a.ind_cdet) = 'N' 
                        or (trim(a.ind_cdet) = 'D' and coalesce(a.pi_cli, b.pct_pi) <> 1) then a.mto_deuda_ope * 0.5
                else 0
            end),0)                                              as exposicion
      ,a.factor_expo
      ,coalesce(a.tipo_garantia, b.cod_tipo_garantia)    as tipo_garantia
      ,coalesce(a.monto_garantia, b.mnt_garantia)        as monto_garantia
      ,a.pvtg
      ,a.pi_metodo_interno
      ,a.pdi_metodo_interno
      ,a.pe_metodo_interno
      ,a.prov_metodo_interno
      ,a.monto_avalado
from """ + db_plat_tempX + """.bci17_basenorma_tmp_boleta_grtia_not_problem a
left join """ + db_plat_tempX + """.tmp_modelo_indiv b
    on a.periodo_id = b.periodo_id
    and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
    and trim(a.cod_cartera_ope)= (CASE WHEN trim(cod_tipo_deu)='NCONT' THEN 'ACT' ELSE 'CTG' END)
    and b.mnt_provision <> 0
where a.periodo_id = """+anomes+"""
"""

# COMMAND ----------

sql_safe(vista_boleta_grtia_not_problem_2)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 8: Eliminacion Boletas de Garantias Tipo Activo 44 para Fecha de Proceso

# COMMAND ----------

# MAGIC %md
# MAGIC Se elimina los datos de la tabla base_normativa_v3_ft correspondiente a la fecha de proceso que se va a insertar posteriormente
# MAGIC
# MAGIC **Tabla de salida**: base_normativa_v3_ft (tabla de salida)

# COMMAND ----------

delete_base_normativa_v3_ft = """delete from """ + db_platinumX + """.base_archivos_normativos where periodo_id = """ + anomes + """ and cod_tabla_89 = 44  """

# COMMAND ----------

sql_safe(delete_base_normativa_v3_ft)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 9: Insercion Datos Tabla base_normativa_v3_ft

# COMMAND ----------

# MAGIC %md
# MAGIC Se prepara e inserta los datos a la tabla base_normativa_v3_ft desde la vista bci17_basenorma_tmp_boleta_grtia_not_problem_2
# MAGIC
# MAGIC **Tabla de salida**: base_normativa_v3_ft (tabla de salida)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Codigo tipo activo
# MAGIC - **nom_activo**: Nombre del tipo activo
# MAGIC - **pct_pi**: Porcentaje de la probabilidad incumplimiento
# MAGIC - **pct_pdi**: Porcentaje de perdida dada por el incumplimiento
# MAGIC - **pct_pe**: Porcentaje de perdida esperada
# MAGIC - **mnt_provision**: Monto provisional
# MAGIC - **cod_calif**: Codigo tipo clasificacion cliente
# MAGIC - **mnt_provision**: Monto de exposicion
# MAGIC - **factor_expo**: Porcentaje factor exposicion
# MAGIC - **des_tipo_gtia**: Tiene o no garantia
# MAGIC - **mnt_garantia**: Monto de la garantia
# MAGIC - **pct_ltv**: Porcentaje PVTG
# MAGIC - **pct_pi_metodo_interno**: Porcentaje probabilidad de incumplimiento metodo interno
# MAGIC - **pct_pdi_metodo_interno**: Porcentaje de perdida dada por el incumplimiento metodo interno
# MAGIC - **pct_pe_metodo_interno**: Porcentaje de perdida esperada metodo interno
# MAGIC - **mnt_prov_metodo_interno**: Monto provision metodo interno
# MAGIC - **mnt_avalado**: Monto avalado del cliente	
# MAGIC - **fec_proceso**: Fecha de cierre

# COMMAND ----------

insert_base_normativa_v3_ft = """ insert into """ + db_platinumX + """.base_archivos_normativos
  select  periodo_id, 				
          rut_cliente, 				
          dv_cliente, 				
          cod_ope_original, 			
          cod_num_operacion, 		
          cod_tip_cart, 				
          cod_tipo_ope, 				
          des_producto_ope, 			
          des_banca_ope, 			
          cod_cartera_ope,			
          mto_deuda_ope,	   			
          num_dias_mora_ope,		
          ind_cdet, 					
          fec_ingreso_deteriodo_ope, 
          cod_cctb, 					
          fl_ope_reneg,
          -998                              as cod_tabla_34,
          tipo_activo					              as cod_tipo_activo,
          nombre_activo				              as nom_activo,
          pi_cli								            as pct_pi,
          pdi_cli								            as pct_pdi,
          pe_cli								            as pct_pe,
          provision						              as mnt_provision,
          tipo_cli					                as cod_calif,
          exposicion						            as mnt_provision,
          factor_expo,
          tipo_garantia                     as des_tipo_gtia,
          monto_garantia                    as mnt_garantia,
          pvtg							                as pct_ltv,
          pi_metodo_interno				          as pct_pi_metodo_interno,
          pdi_metodo_interno				        as pct_pdi_metodo_interno,
          pe_metodo_interno				          as pct_pe_metodo_interno,
          prov_metodo_interno				        as mnt_prov_metodo_interno,
          monto_avalado					            as mnt_avalado,			     
          '""" + date_proceso + """'        as fec_proceso
  from """ + db_plat_tempX + """.bci17_basenorma_tmp_boleta_grtia_not_problem_2
  where periodo_id = """+anomes+"""
"""

# COMMAND ----------

sql_safe(insert_base_normativa_v3_ft)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 10: Obtencion Univ Operaciones Boletas Garantias Con Problemas

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones boletas garantia totalizando la suma de provisiones desde bci17_basenorma_tmp_cli_with_problem y tmp_modelo_com_gr
# MAGIC
# MAGIC **Tabla de salida**: bci17_basenorma_tmp_boleta_grtia_with_problem (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **tipo_activo**: Identificador del tipo activos
# MAGIC - **nombre_activo**: Nombre del tipo activo
# MAGIC - **factor_expo**: Porcentaje factor exposicion
# MAGIC - **pvtg**: Porcentaje PVTG
# MAGIC - **pi_metodo_interno**: Porcentaje probabilidad de incumplimiento interno
# MAGIC - **pdi_metodo_interno**: Porcentaje de perdida dada por el incumplimiento interno
# MAGIC - **pe_metodo_interno**: Porcentaje de perdida esperada interno
# MAGIC - **prov_metodo_interno**: Monto provision metodo interno
# MAGIC - **monto_avalado**: Monto avalado del cliente

# COMMAND ----------

vista_boleta_grtia_with_problem = """ insert into """ + db_plat_tempX + """.bci17_basenorma_tmp_boleta_grtia_with_problem
select  a.periodo_id		
      ,a.rut_cliente
      ,a.dv_cliente 				
      ,a.cod_ope_original
      ,a.cod_num_operacion
      ,a.cod_tip_cart			
      ,a.cod_tipo_ope 				
      ,a.des_producto_ope
      ,a.des_banca_ope		
      ,a.cod_cartera_ope
      ,a.mto_deuda_ope	   			
      ,a.num_dias_mora_ope
      ,a.ind_cdet		
      ,a.fec_ingreso_deteriodo_ope
      ,a.cod_cctb
      ,a.fl_ope_reneg
      ,44                                   as tipo_activo
      ,'BOLETAS GARANTIAS'                  as nombre_activo
      ,b.pct_pi                             as pi_cli
      ,b.pct_pdi	        			            as pdi_cli
      ,b.pct_pe			        	              as pe_cli
      ,sum(b.mnt_provision)		              as provision
      ,b.cod_calificacion                   as tipo_cli
      ,0                                    as exposicion
      ,0.5                                  as factor_expo
      ,b.cod_tipo_garantia                  as tipo_garantia
      ,b.mnt_garantia                       as monto_garantia
      ,0                                    as pct_ltv
      ,0                                    as pi_metodo_interno
      ,0                                    as pdi_metodo_interno
      ,0                                    as pe_metodo_interno
      ,0                                    as prov_metodo_interno
      ,0                                    as monto_avalado
from """ + db_plat_tempX + """.bci17_basenorma_tmp_cli_with_problem a
left join """ + db_plat_tempX + """.tmp_modelo_indiv b
    on a.periodo_id = b.periodo_id
    and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
    and trim(a.cod_cartera_ope)= (CASE WHEN trim(cod_tipo_deu)='NCONT' THEN 'ACT' ELSE 'CTG' END)
    and b.mnt_provision <> 0
where a.periodo_id = """+anomes+"""
group by a.periodo_id		
      ,a.rut_cliente
      ,a.dv_cliente 				
      ,a.cod_ope_original
      ,a.cod_num_operacion
      ,a.cod_tip_cart			
      ,a.cod_tipo_ope 				
      ,a.des_producto_ope
      ,a.des_banca_ope		
      ,a.cod_cartera_ope
      ,a.mto_deuda_ope	   			
      ,a.num_dias_mora_ope
      ,a.ind_cdet		
      ,a.fec_ingreso_deteriodo_ope
      ,a.cod_cctb
      ,a.fl_ope_reneg
      ,b.pct_pi                             
      ,b.pct_pdi	        			            
      ,b.pct_pe			        	              
      ,b.cod_calificacion                   
      ,b.cod_tipo_garantia                  
      ,b.mnt_garantia 
"""

# COMMAND ----------

sql_safe(vista_boleta_grtia_with_problem)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 11:Obtencion Maxima PI Cliente

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones boletas garantia Maximo PI desde paso anterior
# MAGIC
# MAGIC **Tabla de salida**: bci17_tmp_boleta_grtia_with_problem_resp (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **tipo_activo**: Identificador del tipo activos
# MAGIC - **nombre_activo**: Nombre del tipo activo
# MAGIC - **factor_expo**: Porcentaje factor exposicion
# MAGIC - **pvtg**: Porcentaje Loan To Value
# MAGIC - **pi_metodo_interno**: Porcentaje probabilidad de incumplimiento interno
# MAGIC - **pdi_metodo_interno**: Porcentaje de perdida dada por el incumplimiento interno
# MAGIC - **pe_metodo_interno**: Porcentaje de perdida esperada interno
# MAGIC - **prov_metodo_interno**: Monto provision metodo interno
# MAGIC - **monto_avalado**: Monto avalado del cliente

# COMMAND ----------

vista_boleta_gtia_max_pi_cli = """ insert into """ + db_plat_tempX + """.bci17_tmp_boleta_grtia_with_problem_resp
select periodo_id		
      ,rut_cliente
      ,dv_cliente 				
      ,cod_ope_original
      ,cod_num_operacion
      ,cod_tip_cart			
      ,cod_tipo_ope 				
      ,des_producto_ope
      ,des_banca_ope		
      ,cod_cartera_ope
      ,mto_deuda_ope	   			
      ,num_dias_mora_ope
      ,ind_cdet		
      ,fec_ingreso_deteriodo_ope
      ,cod_cctb
      ,fl_ope_reneg
      ,tipo_activo
      ,nombre_activo
      ,max(pi_cli) as pi_cli
      ,pdi_cli
      ,pe_cli
      ,provision
      ,tipo_cli
      ,exposicion
      ,factor_expo
      ,tipo_garantia
      ,monto_garantia
      ,pct_ltv
      ,pi_metodo_interno
      ,pdi_metodo_interno
      ,pe_metodo_interno
      ,prov_metodo_interno
      ,monto_avalado
from """ + db_plat_tempX + """.bci17_basenorma_tmp_boleta_grtia_with_problem
where periodo_id = """ +anomes + """
group by periodo_id, rut_cliente, dv_cliente, cod_ope_original, cod_num_operacion
      ,cod_tip_cart, cod_tipo_ope, des_producto_ope, des_banca_ope, cod_cartera_ope
      ,mto_deuda_ope, num_dias_mora_ope, ind_cdet, fec_ingreso_deteriodo_ope
      ,cod_cctb, fl_ope_reneg, tipo_activo, nombre_activo, pdi_cli, pe_cli, provision
      ,tipo_cli, exposicion, factor_expo, tipo_garantia, monto_garantia, pct_ltv
      ,pi_metodo_interno, pdi_metodo_interno, pe_metodo_interno, prov_metodo_interno
      , monto_avalado
"""

# COMMAND ----------

sql_safe(vista_boleta_gtia_max_pi_cli)

# COMMAND ----------

qry_dlt_tabla_1 = """
delete from """+db_plat_tempX+""".bci17_basenorma_tmp_boleta_grtia_with_problem where periodo_id= """+anomes+"""  """

sql_safe(qry_dlt_tabla_1)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 12: Obtencion Maximo PDI Cliente

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones boletas garantia por  maximo PDI desde paso anterior
# MAGIC
# MAGIC **Tabla de salida**: bci17_basenorma_tmp_boleta_grtia_with_problem (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **tipo_activo**: Identificador del tipo activos
# MAGIC - **nombre_activo**: Nombre del tipo activo
# MAGIC - **factor_expo**: Porcentaje factor exposicion
# MAGIC - **pvtg**: Porcentaje Loan To Value
# MAGIC - **pi_metodo_interno**: Porcentaje probabilidad de incumplimiento interno
# MAGIC - **pdi_metodo_interno**: Porcentaje de perdida dada por el incumplimiento interno
# MAGIC - **pe_metodo_interno**: Porcentaje de perdida esperada interno
# MAGIC - **prov_metodo_interno**: Monto provision metodo interno
# MAGIC - **monto_avalado**: Monto avalado del cliente

# COMMAND ----------

vista_boleta_gtia_max_pdi_cli = """ insert into """ + db_plat_tempX + """.bci17_basenorma_tmp_boleta_grtia_with_problem
select periodo_id		
      ,rut_cliente
      ,dv_cliente 				
      ,cod_ope_original
      ,cod_num_operacion
      ,cod_tip_cart			
      ,cod_tipo_ope 				
      ,des_producto_ope
      ,des_banca_ope		
      ,cod_cartera_ope
      ,mto_deuda_ope	   			
      ,num_dias_mora_ope
      ,ind_cdet		
      ,fec_ingreso_deteriodo_ope
      ,cod_cctb
      ,fl_ope_reneg
      ,tipo_activo
      ,nombre_activo
      ,pi_cli
      ,max(pdi_cli) as pdi_cli
      ,pe_cli
      ,provision
      ,tipo_cli
      ,exposicion
      ,factor_expo
      ,tipo_garantia
      ,monto_garantia
      ,pct_ltv
      ,pi_metodo_interno
      ,pdi_metodo_interno
      ,pe_metodo_interno
      ,prov_metodo_interno
      ,monto_avalado
from  """ + db_plat_tempX + """.bci17_tmp_boleta_grtia_with_problem_resp
where periodo_id = """ + anomes + """
group by periodo_id, rut_cliente, dv_cliente, cod_ope_original, cod_num_operacion
      ,cod_tip_cart, cod_tipo_ope, des_producto_ope, des_banca_ope, cod_cartera_ope
      ,mto_deuda_ope, num_dias_mora_ope, ind_cdet, fec_ingreso_deteriodo_ope
      ,cod_cctb, fl_ope_reneg, tipo_activo, nombre_activo, pi_cli, pe_cli, provision
      ,tipo_cli, exposicion, factor_expo, tipo_garantia, monto_garantia, pct_ltv
      ,pi_metodo_interno, pdi_metodo_interno, pe_metodo_interno, prov_metodo_interno
      , monto_avalado
  """

# COMMAND ----------

sql_safe(vista_boleta_gtia_max_pdi_cli)

# COMMAND ----------

qry_dlt_tabla_resp = """
delete from  """+db_plat_tempX+""".bci17_tmp_boleta_grtia_with_problem_resp where periodo_id= """+anomes+"""  """

sql_safe(qry_dlt_tabla_resp)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 13: Obtencion Maxima PE

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones boletas garantia por  maximo PE desde paso anterior
# MAGIC
# MAGIC **Tabla de salida**: bci17_tmp_boleta_grtia_with_problem_resp (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **tipo_activo**: Identificador del tipo activos
# MAGIC - **nombre_activo**: Nombre del tipo activo
# MAGIC - **factor_expo**: Porcentaje factor exposicion
# MAGIC - **pvtg**: Porcentaje Loan To Value
# MAGIC - **pi_metodo_interno**: Porcentaje probabilidad de incumplimiento interno
# MAGIC - **pdi_metodo_interno**: Porcentaje de perdida dada por el incumplimiento interno
# MAGIC - **pe_metodo_interno**: Porcentaje de perdida esperada interno
# MAGIC - **prov_metodo_interno**: Monto provision metodo interno
# MAGIC - **monto_avalado**: Monto avalado del cliente

# COMMAND ----------

vista_boleta_gtia_max_pe_cli = """ insert into """ + db_plat_tempX + """.bci17_tmp_boleta_grtia_with_problem_resp
select periodo_id		
      ,rut_cliente
      ,dv_cliente 				
      ,cod_ope_original
      ,cod_num_operacion
      ,cod_tip_cart			
      ,cod_tipo_ope 				
      ,des_producto_ope
      ,des_banca_ope		
      ,cod_cartera_ope
      ,mto_deuda_ope	   			
      ,num_dias_mora_ope
      ,ind_cdet		
      ,fec_ingreso_deteriodo_ope
      ,cod_cctb
      ,fl_ope_reneg
      ,tipo_activo
      ,nombre_activo
      ,pi_cli
      ,pdi_cli
      ,max(pe_cli) as pe_cli
      ,provision
      ,tipo_cli
      ,exposicion
      ,factor_expo
      ,tipo_garantia
      ,monto_garantia
      ,pct_ltv
      ,pi_metodo_interno
      ,pdi_metodo_interno
      ,pe_metodo_interno
      ,prov_metodo_interno
      ,monto_avalado
from """ + db_plat_tempX + """.bci17_basenorma_tmp_boleta_grtia_with_problem
where periodo_id =  """ +anomes + """
group by periodo_id, rut_cliente, dv_cliente, cod_ope_original, cod_num_operacion
      ,cod_tip_cart, cod_tipo_ope, des_producto_ope, des_banca_ope, cod_cartera_ope
      ,mto_deuda_ope, num_dias_mora_ope, ind_cdet, fec_ingreso_deteriodo_ope
      ,cod_cctb, fl_ope_reneg, tipo_activo, nombre_activo, pi_cli, pdi_cli, provision
      ,tipo_cli, exposicion, factor_expo, tipo_garantia, monto_garantia, pct_ltv
      ,pi_metodo_interno, pdi_metodo_interno, pe_metodo_interno, prov_metodo_interno
      , monto_avalado
"""

# COMMAND ----------

sql_safe(vista_boleta_gtia_max_pe_cli)

# COMMAND ----------

qry_dlt_tabla_1 = """
delete from """+db_plat_tempX+""".bci17_basenorma_tmp_boleta_grtia_with_problem where periodo_id= """+anomes+"""  """

sql_safe(qry_dlt_tabla_1)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 14: Obtecion Por Maximo Tipo Cliente

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones boletas garantia por  maximo Tipo Cliente desde paso anterior
# MAGIC
# MAGIC **Tabla de salida**: basenorma_tmp_boleta_grtia_with_problem (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **tipo_activo**: Identificador del tipo activos
# MAGIC - **nombre_activo**: Nombre del tipo activo
# MAGIC - **factor_expo**: Porcentaje factor exposicion
# MAGIC - **pvtg**: Porcentaje Loan To Value
# MAGIC - **pi_metodo_interno**: Porcentaje probabilidad de incumplimiento interno
# MAGIC - **pdi_metodo_interno**: Porcentaje de perdida dada por el incumplimiento interno
# MAGIC - **pe_metodo_interno**: Porcentaje de perdida esperada interno
# MAGIC - **prov_metodo_interno**: Monto provision metodo interno
# MAGIC - **monto_avalado**: Monto avalado del cliente

# COMMAND ----------

vista_boleta_gtia_max_tip_cli = """ insert into """ + db_plat_tempX + """.bci17_basenorma_tmp_boleta_grtia_with_problem
select periodo_id		
      ,rut_cliente
      ,dv_cliente 				
      ,cod_ope_original
      ,cod_num_operacion
      ,cod_tip_cart			
      ,cod_tipo_ope 				
      ,des_producto_ope
      ,des_banca_ope		
      ,cod_cartera_ope
      ,mto_deuda_ope	   			
      ,num_dias_mora_ope
      ,ind_cdet		
      ,fec_ingreso_deteriodo_ope
      ,cod_cctb
      ,fl_ope_reneg
      ,tipo_activo
      ,nombre_activo
      ,pi_cli
      ,pdi_cli
      ,pe_cli
      ,provision
      ,max(trim(tipo_cli)) as tipo_cli
      ,exposicion
      ,factor_expo
      ,tipo_garantia
      ,monto_garantia
      ,pct_ltv
      ,pi_metodo_interno
      ,pdi_metodo_interno
      ,pe_metodo_interno
      ,prov_metodo_interno
      ,monto_avalado
from  """ + db_plat_tempX + """.bci17_tmp_boleta_grtia_with_problem_resp
where periodo_id = """ + anomes + """
group by periodo_id, rut_cliente, dv_cliente, cod_ope_original, cod_num_operacion
      ,cod_tip_cart, cod_tipo_ope, des_producto_ope, des_banca_ope, cod_cartera_ope
      ,mto_deuda_ope, num_dias_mora_ope, ind_cdet, fec_ingreso_deteriodo_ope
      ,cod_cctb, fl_ope_reneg, tipo_activo, nombre_activo, pi_cli, pdi_cli, pe_cli
      ,provision, exposicion, factor_expo, tipo_garantia, monto_garantia, pct_ltv
      ,pi_metodo_interno, pdi_metodo_interno, pe_metodo_interno, prov_metodo_interno
      , monto_avalado
 """

# COMMAND ----------

sql_safe(vista_boleta_gtia_max_tip_cli)

# COMMAND ----------

qry_dlt_tabla_resp = """
delete from  """+db_plat_tempX+""".bci17_tmp_boleta_grtia_with_problem_resp where periodo_id= """+anomes+"""  """

sql_safe(qry_dlt_tabla_resp)

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones boleta garantias con duplicidad desde bci17_basenorma_tmp_boleta_grtia_with_problem y tmp_modelo_indiv
# MAGIC
# MAGIC **Tabla de salida**: bci17_basenorma_tmp_boleta_grtia_with_problem_2 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **tipo_activo**: Identificador del tipo activos
# MAGIC - **nombre_activo**: Nombre del tipo activo
# MAGIC - **pi_cli**: Porcentaje probabilidad de incumplimiento cliente
# MAGIC - **pdi_cli**: Porcentaje de perdida dada por el incumplimiento cliente
# MAGIC - **pe_cli**: Porcentaje de perdida esperada cliente
# MAGIC - **provision**: Monto provision cliente
# MAGIC - **tipo_cli**: Codigo tipo de cliente
# MAGIC - **exposicion**: Monto de exposicion
# MAGIC - **factor_expo**: Porcentaje factor exposicion
# MAGIC - **tipo_garantia**: Codigo del tipo de garantia cliente
# MAGIC - **monto_garantia**: Monto de garantia cliente
# MAGIC - **pvtg**: Porcentaje PVTG
# MAGIC - **pi_metodo_interno**: Porcentaje probabilidad de incumplimiento interno
# MAGIC - **pdi_metodo_interno**: Porcentaje de perdida dada por el incumplimiento interno
# MAGIC - **pe_metodo_interno**: Porcentaje de perdida esperada interno
# MAGIC - **prov_metodo_interno**: Monto provision metodo interno
# MAGIC - **monto_avalado**: Monto avalado del cliente

# COMMAND ----------

vista_boleta_grtia_with_problem_2 = """ insert into """ + db_plat_tempX + """.bci17_basenorma_tmp_boleta_grtia_with_problem_2
  select  distinct
          a.periodo_id, 				
          a.rut_cliente, 				
          a.dv_cliente, 				
          a.cod_ope_original, 			
          a.cod_num_operacion, 		
          a.cod_tip_cart, 				
          a.cod_tipo_ope, 				
          a.des_producto_ope, 			
          a.des_banca_ope, 			
          a.cod_cartera_ope,			
          a.mto_deuda_ope,	   			
          a.num_dias_mora_ope,		
          a.ind_cdet, 					
          a.fec_ingreso_deteriodo_ope, 
          a.cod_cctb, 					
          a.fl_ope_reneg,
          a.tipo_activo,
          a.nombre_activo,
          max(b.pct_pi)                             as pi_cli,
          max(b.pct_pdi)	        			            as pdi_cli,
          max(b.pct_pe)			        	              as pe_cli,
          sum(b.mnt_provision)		                  as provision,
          max(b.cod_calificacion)                   as tipo_cli,
          round(case when a.ind_cdet = 'N' or (a.ind_cdet = 'D' and max(b.pct_pi) != 1) then a.mto_deuda_ope * 0.5
                else 0
          end)                                      as exposicion,
          a.factor_expo,
          b.cod_tipo_garantia                       as tipo_garantia,
          b.mnt_garantia                            as monto_garantia,
          a.pvtg,
          a.pi_metodo_interno,
          a.pdi_metodo_interno,
          a.pe_metodo_interno,
          a.prov_metodo_interno,
          a.monto_avalado
  from """ + db_plat_tempX + """.bci17_basenorma_tmp_boleta_grtia_with_problem a
  left join """ + db_plat_tempX + """.tmp_modelo_indiv b
    on a.cod_num_operacion = b.cod_num_operacion
      and a.periodo_id = b.periodo_id
      and b.mnt_provision != 0
  group by  a.periodo_id, a.rut_cliente, a.dv_cliente, a.cod_ope_original, a.cod_num_operacion, a.cod_tip_cart, a.cod_tipo_ope, a.des_producto_ope, 
            a.des_banca_ope, a.cod_cartera_ope,	a.mto_deuda_ope, a.num_dias_mora_ope,	a.ind_cdet, a.fec_ingreso_deteriodo_ope, a.cod_cctb, a.fl_ope_reneg,
            a.tipo_activo, a.nombre_activo, a.factor_expo, b.cod_tipo_garantia, b.mnt_garantia, a.pvtg, a.pi_metodo_interno, a.pdi_metodo_interno, 
            a.pe_metodo_interno, a.prov_metodo_interno, a.monto_avalado
"""

# COMMAND ----------

# MAGIC %md
# MAGIC sqlsafe(vista_boleta_grtia_with_problem_2)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 15: Insercion Datos Tabla base_normativa_v3_ft

# COMMAND ----------

# MAGIC %md
# MAGIC Se prepara e inserta los datos a la tabla base_normativa_v3_ft desde la vista bci17_basenorma_tmp_boleta_grtia_not_problem_2
# MAGIC
# MAGIC **Tabla de salida**: base_normativa_v3_ft (tabla de salida)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Codigo tipo activo
# MAGIC - **nom_activo**: Nombre del tipo activo
# MAGIC - **pct_pi**: Porcentaje de la probabilidad incumplimiento
# MAGIC - **pct_pdi**: Porcentaje de perdida dada por el incumplimiento
# MAGIC - **pct_pe**: Porcentaje de perdida esperada
# MAGIC - **mnt_provision**: Monto provisional
# MAGIC - **cod_calif**: Codigo tipo clasificacion cliente
# MAGIC - **mnt_provision**: Monto de exposicion
# MAGIC - **factor_expo**: Porcentaje factor exposicion
# MAGIC - **des_tipo_gtia**: Tiene o no garantia
# MAGIC - **mnt_garantia**: Monto de la garantia
# MAGIC - **pct_ltv**: Porcentaje PVTG
# MAGIC - **pct_pi_metodo_interno**: Porcentaje probabilidad de incumplimiento metodo interno
# MAGIC - **pct_pdi_metodo_interno**: Porcentaje de perdida dada por el incumplimiento metodo interno
# MAGIC - **pct_pe_metodo_interno**: Porcentaje de perdida esperada metodo interno
# MAGIC - **mnt_prov_metodo_interno**: Monto provision metodo interno
# MAGIC - **mnt_avalado**: Monto avalado del cliente	
# MAGIC - **fec_proceso**: Fecha de cierre

# COMMAND ----------

insert_base_normativa_v3_ft_2 = """ insert into """ + db_platinumX + """.base_archivos_normativos
  select  periodo_id, 				
          rut_cliente, 				
          dv_cliente, 				
          cod_ope_original, 			
          cod_num_operacion, 		
          cod_tip_cart, 				
          cod_tipo_ope, 				
          des_producto_ope, 			
          des_banca_ope, 			
          cod_cartera_ope,			
          mto_deuda_ope,	   			
          num_dias_mora_ope,		
          ind_cdet, 					
          fec_ingreso_deteriodo_ope, 
          cod_cctb, 					
          fl_ope_reneg,
          -998                              as cod_tabla_34,
          tipo_activo					              as cod_tipo_activo,
          nombre_activo				              as nom_activo,
          pi_cli								            as pct_pi,
          pdi_cli								            as pct_pdi,
          pe_cli								            as pct_pe,
          provision						              as mnt_provision,
          tipo_cli					                as cod_calif,
          round((CASE WHEN trim(ind_cdet) = 'N' OR (trim(ind_cdet) = 'D' AND pi_cli <> 1) THEN (mto_deuda_ope * 0.5) ELSE 0 END),0)						            as mnt_exposicion,
          factor_expo,
          tipo_garantia                     as des_tipo_gtia,
          monto_garantia                    as mnt_garantia,
          pct_ltv							                as pct_ltv,
          pi_metodo_interno				          as pct_pi_metodo_interno,
          pdi_metodo_interno				        as pct_pdi_metodo_interno,
          pe_metodo_interno				          as pct_pe_metodo_interno,
          prov_metodo_interno				        as mnt_prov_metodo_interno,
          monto_avalado					            as mnt_avalado,			     
          '""" + date_proceso + """'        as fec_proceso
  from """ + db_plat_tempX + """.bci17_basenorma_tmp_boleta_grtia_with_problem
  where periodo_id = """+anomes+"""
"""

# COMMAND ----------

sql_safe(insert_base_normativa_v3_ft_2)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Conteo de registros insertados en tabla de salida

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """ + db_platinumX + """.base_archivos_normativos  where periodo_id = """ + anomes + """ and cod_tabla_89 = 44 """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal base_archivos_normativos para BOLETAS GARANTIAS ")

# COMMAND ----------

# MAGIC %md
# MAGIC # Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")